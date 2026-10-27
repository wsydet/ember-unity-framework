#!/usr/bin/env node
// Copyright (c) 2026 Burner Games. All rights reserved.
// This file is part of Burner Unity Packages. Package: com.burner.ui.prefabmaker
'use strict';
const fs = require('node:fs/promises');
const path = require('node:path');
const crypto = require('node:crypto');
const ID = /^[a-f0-9]{32}$/;
const hash = bytes => crypto.createHash('sha256').update(bytes).digest('hex');
const fail = (code, message = code) => Object.assign(new Error(message), {code});
async function read(file, max = 64 * 1024 * 1024) {
  const handle = await fs.open(file, 'r');
  try {
    if ((await handle.stat()).size > max) throw fail('FILE_TOO_LARGE', file);
    const bytes = await handle.readFile();
    if (bytes.length > max) throw fail('FILE_TOO_LARGE', file);
    return {bytes, value: JSON.parse(bytes.toString('utf8'))};
  } finally {await handle.close();}
}
async function optional(file, max) {try {return await read(file, max);} catch (error) {if (error.code === 'ENOENT') return null; throw error;}}
async function publish(file, bytes) {
  const tmp = file + '.' + crypto.randomUUID() + '.tmp';
  const handle = await fs.open(tmp, 'wx');
  try {await handle.writeFile(bytes); await handle.sync();} finally {await handle.close();}
  try {await fs.link(tmp, file);} finally {await fs.unlink(tmp);}
}
async function publishRequest(file, bytes) {
  try {await publish(file, bytes);}
  catch (error) {
    if (error.code !== 'EEXIST') throw error;
    const existing = await read(file, 65536);
    if (hash(existing.bytes) !== hash(bytes)) throw fail('REQUEST_ID_CONFLICT', 'Existing request ID has different bytes. Do not replace or replay it.');
  }
}
const limited = (value, count) => String(value ?? '').slice(0, count);
function summaryText(value) {
  // JSON escaping and multibyte characters count against the byte budget too.
  let output = JSON.stringify(value);
  if (Buffer.byteLength(output) <= 8191) return output;
  const essential = {requestId: value.requestId, status: value.status, productSaved: value.productSaved,
    recordStatus: value.recordStatus, sessionId: value.sessionId,
    partialOutputPossible: value.error?.partialOutputPossible === true || value.value?.partialOutputPossible === true,
      error: value.error ? {code: limited(value.error.code, 128), message: limited(value.error.message, 256)} : null,
    details: value.details, truncated: true, nextAction: value.nextAction};
  output = JSON.stringify(essential);
  if (Buffer.byteLength(output) > 8191) throw fail('SUMMARY_LIMIT', 'Details path exceeds the summary budget. Use a shorter communication directory.');
  return output;
}
async function query(root, id) {
  if (!ID.test(id)) throw fail('INVALID_REQUEST_ID');
  const request = await read(path.join(root, 'requests', id + '.json'), 65536);
  const requestHash = hash(request.bytes);
  const detailsPath = path.join(root, 'results', id + '.json');
  const result = await optional(detailsPath);
  if (result) {
    const value = result.value;
    if (value.version !== 1 || value.requestId !== id || value.requestHash !== requestHash || value.operation !== request.value.operation || !ID.test(value.sessionId) ||
        !['completed', 'cancelled', 'rejected', 'failed', 'unknown'].includes(value.status)) throw fail('RESULT_IDENTITY_MISMATCH');
    return {requestId: id, status: value.status, productSaved: value.productSaved, recordStatus: value.value?.recordStatus ?? null,
      recordWarning: limited(value.value?.recordWarning, 512) || null, productFileCount: value.value?.productFiles?.length ?? 0, sessionId: value.sessionId,
      partialOutputPossible: value.error?.partialOutputPossible === true || value.value?.partialOutputPossible === true,
      error: value.error ? {code: limited(value.error.code, 128), message: limited(value.error.message, 1024)} : null,
      pluginVersion: limited(value.value?.pluginVersion, 64), document: value.value?.document ? {
        id: value.value.document.id, path: String(value.value.document.path || '').slice(0, 1024), saved: value.value.document.saved
      } : null, operations: value.value?.operations?.slice(0, 8).map(item => limited(item, 64)), targetCount: value.value?.targets?.length,
      details: {path: detailsPath, sha256: hash(result.bytes), bytes: result.bytes.length}, truncated: true,
      nextAction: value.status === 'completed' ? 'read-details' : value.status === 'unknown' ? 'inspect-original-request-and-products; do-not-replay' : 'resolve-error-before-a-new-explicit-request'};
  }
  const claim = await optional(path.join(root, 'claims', id + '.json'));
  if (claim && (claim.value.requestId !== id || claim.value.requestHash !== requestHash)) throw fail('CLAIM_IDENTITY_MISMATCH');
  const host = await optional(path.join(root, 'host.json'));
  const age = host ? Date.now() - Date.parse(host.value.observedAt) : Infinity;
  const hostState = host?.value.state === 'disabled' ? 'disabled' : !Number.isFinite(age) || age > 10000 ? 'unavailable-or-busy' : host?.value.state;
  return {requestId: id, status: claim ? 'pending-claimed' : 'pending', hostState,
    nextAction: 'query-original-id; timeout-does-not-cancel-or-authorize-replay'};
}
function args(argv) {
  const result = {command: argv.shift()};
  while (argv.length) {const key = argv.shift(); if (!/^--[a-z-]+$/.test(key) || !argv.length) throw fail('INVALID_ARGUMENTS'); if (key.slice(2) in result) throw fail('DUPLICATE_ARGUMENT'); result[key.slice(2)] = argv.shift();}
  return result;
}
async function main(argv) {
  const options = args(argv);
  if (!['inspect', 'query', 'call'].includes(options.command) || Boolean(options.root) === Boolean(options.directory))
    throw fail('USAGE', 'node bridge.js inspect --root <PSD-folder> [--wait-ms 5000] | query --root <PSD-folder> --id <requestId> | call --root <PSD-folder> --request <json>; --directory is for frozen legacy jobs only');
  const allowed = new Set(['command', 'root', 'directory', 'wait-ms', ...(options.command === 'query' ? ['id'] : options.command === 'call' ? ['request'] : [])]);
  if (Object.keys(options).some(key => !allowed.has(key))) throw fail('INVALID_ARGUMENTS');
  const wait = Number(options['wait-ms'] ?? 0);
  if (!Number.isInteger(wait) || wait < 0 || wait > 60000) throw fail('INVALID_WAIT');
  const root = options.root ? path.resolve(options.root, '.local/maker/bridge') : path.resolve(options.directory);
  let id;
  if (options.command === 'query') id = options.id;
  else {
    let request;
    if (options.command === 'call') {
      const source = await read(path.resolve(options.request), 65536); request = source.value;
      if (request.version !== 1 || !ID.test(request.requestId)) throw fail('INVALID_REQUEST');
      // Keep the exact request bytes and ID. A retry never silently creates a new write.
      id = request.requestId;
      await publishRequest(path.join(root, 'requests', id + '.json'), source.bytes);
    } else {
      id = crypto.randomUUID().replace(/-/g, '');
      request = {version: 1, requestId: id, operation: 'inspect', createdAt: new Date().toISOString(), startBefore: new Date(Date.now() + 60000).toISOString(), params: {}};
      await publish(path.join(root, 'requests', id + '.json'), Buffer.from(JSON.stringify(request)));
    }
    console.error('requestId=' + id + '; query this ID after timeout; do not replay.');
  }
  const deadline = Date.now() + wait;
  let result;
  do {
    result = await query(root, id);
    if (!result.status.startsWith('pending') || Date.now() >= deadline) break;
    await new Promise(resolve => setTimeout(resolve, 200));
  } while (true);
  if (result.status.startsWith('pending') && wait) result.waitTimedOut = true;
  const output = summaryText(result);
  console.log(output);
  return result.status === 'completed' ? 0 : result.status.startsWith('pending') || result.status === 'unknown' ? 3 : 2;
}
if (require.main === module) main(process.argv.slice(2)).then(code => {process.exitCode = code;}).catch(error => {
  console.log(summaryText({status: 'error', error: {code: error.code || 'CLI_ERROR', message: limited(error.message, 1024)}, nextAction: 'inspect-existing-request; do-not-replay-unknown-work'})); process.exitCode = 2;
});
module.exports = {query, publish, publishRequest, summaryText, main};
