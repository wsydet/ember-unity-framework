// Ember Photoshop document adapter. Called with EMBER_REQUEST by run-photoshop.ps1.
// Native Photoshop DOM only; source artwork is never flattened or overwritten.
(function () {
    function json(v) {
        if (v === null) return 'null';
        if (typeof v === 'string') return '"' + v.replace(/\\/g, '\\\\').replace(/"/g, '\\"').replace(/\r/g, '\\r').replace(/\n/g, '\\n').replace(/\t/g, '\\t') + '"';
        if (typeof v !== 'object') return String(v);
        var a = [], k;
        if (v instanceof Array) { for (k = 0; k < v.length; k++) a.push(json(v[k])); return '[' + a.join(',') + ']'; }
        for (k in v) if (v.hasOwnProperty(k)) a.push(json(k) + ':' + json(v[k]));
        return '{' + a.join(',') + '}';
    }
    function write(file, value) { var f = new File(file); f.encoding = 'UTF8'; if (!f.open('w')) throw Error('REPORT_WRITE_FAILED'); f.write(json(value)); f.close(); }
    function color(hex) { var c = new SolidColor(); c.rgb.hexValue = hex; return c; }
    function snapshot(doc) {
        var rows = [];
        function walk(parent, path) {
            for (var i = 0; i < parent.layers.length; i++) {
                var l = parent.layers[i], p = path ? path + '/' + l.name : l.name;
                var b = l.bounds, r = { id: String(l.id), name: l.name, path: p, visible: l.visible, kind: l.typename,
                    bounds: [b[0].as('px'), b[1].as('px'), b[2].as('px'), b[3].as('px')] };
                if (l.typename === 'ArtLayer' && l.kind === LayerKind.TEXT) { r.kind = 'text'; r.text = l.textItem.contents; r.font = l.textItem.font; }
                rows.push(r); if (l.typename === 'LayerSet') walk(l, p);
            }
        }
        walk(doc, ''); return { photoshopVersion: app.version, path: doc.fullName.fsName, width: doc.width.as('px'), height: doc.height.as('px'), saved: doc.saved, layers: rows };
    }
    var q = EMBER_REQUEST, doc, previousUnits = app.preferences.rulerUnits, previousDialogs = app.displayDialogs;
    try {
        app.preferences.rulerUnits = Units.PIXELS; app.displayDialogs = DialogModes.NO;
        if (q.operation === 'create') {
            if (new File(q.output).exists) throw Error('OUTPUT_EXISTS');
            doc = app.documents.add(q.width, q.height, 72, q.name, NewDocumentMode.RGB, DocumentFill.TRANSPARENT);
            var mappings = [];
            function build(spec, parent) {
                var layer, b = spec.rect;
                if (spec.children) {
                    layer = parent.layerSets.add(); layer.name = spec.name;
                    for (var i = 0; i < spec.children.length; i++) build(spec.children[i], layer);
                } else if (spec.image) {
                    var source = app.open(new File(spec.image));
                    try { layer = source.activeLayer.duplicate(doc, ElementPlacement.PLACEATBEGINNING); }
                    finally { source.close(SaveOptions.DONOTSAVECHANGES); }
                    app.activeDocument = doc; layer.move(parent, ElementPlacement.INSIDE); layer.name = spec.name;
                    var bounds = layer.bounds, w = bounds[2].as('px') - bounds[0].as('px'), h = bounds[3].as('px') - bounds[1].as('px');
                    if (b) { layer.resize(b[2] / w * 100, b[3] / h * 100, AnchorPosition.TOPLEFT); bounds = layer.bounds; layer.translate(b[0] - bounds[0].as('px'), b[1] - bounds[1].as('px')); }
                } else {
                    layer = parent.artLayers.add(); layer.name = spec.name; doc.activeLayer = layer;
                    if (spec.text !== undefined) {
                        layer.kind = LayerKind.TEXT; var t = layer.textItem;
                        t.kind = TextType.PARAGRAPHTEXT; t.position = [b[0], b[1]]; t.width = b[2]; t.height = b[3];
                        t.size = spec.size || 24; t.font = spec.font || 'ArialMT'; t.color = color(spec.color || '1D1D1F'); t.contents = spec.text;
                    } else {
                        doc.selection.select([[b[0], b[1]], [b[0]+b[2], b[1]], [b[0]+b[2], b[1]+b[3]], [b[0], b[1]+b[3]]]);
                        doc.selection.fill(color(spec.color || 'FFFFFF')); doc.selection.deselect();
                    }
                }
                layer.visible = spec.visible !== false;
                mappings.push({id: String(layer.id), before: spec.name, after: spec.exportName, semantic: spec.semantic || '', sourceImage: spec.image || ''});
                return layer;
            }
            for (var i = 0; i < q.layers.length; i++) build(q.layers[i], doc);
            var options = new PhotoshopSaveOptions(); options.layers = true; options.embedColorProfile = true;
            doc.saveAs(new File(q.output), options, false, Extension.LOWERCASE);
            write(q.report, {status: 'complete', operation: q.operation, document: snapshot(doc), mappings: mappings});
            doc.close(SaveOptions.DONOTSAVECHANGES);
        } else {
            var wanted = new File(q.input).fsName.toLowerCase();
            for (var j = 0; j < app.documents.length; j++) {
                var opened = app.documents[j];
                try { if (opened.fullName.fsName.toLowerCase() === wanted) { doc = opened; break; } } catch (_) {}
            }
            if (!doc) doc = app.open(new File(q.input));
            if (!doc.saved) throw Error('UNSAVED_DOCUMENT');
            app.activeDocument = doc;
            if (q.operation === 'rename') {
                if (!/\.renamed\.psd$/i.test(q.input)) throw Error('WORKING_COPY_REQUIRED');
                var layers = {}, before = snapshot(doc);
                function index(parent) { for (var n = 0; n < parent.layers.length; n++) { var l = parent.layers[n]; layers[String(l.id)] = l; if (l.typename === 'LayerSet') index(l); } }
                index(doc);
                for (var n = 0; n < q.mappings.length; n++) { var m = q.mappings[n]; if (!layers[m.id] || layers[m.id].name !== m.before || !m.after) throw Error('LAYER_IDENTITY_CHANGED: ' + m.id); }
                for (var n = 0; n < q.mappings.length; n++) layers[q.mappings[n].id].name = q.mappings[n].after;
                // Photoshop name assignment can reveal hidden layers; restore visibility last, children before parents.
                var visibility = q.visibility || before.layers;
                for (var v = visibility.length - 1; v >= 0; v--) { if (!layers[visibility[v].id]) throw Error("VISIBILITY_ID_MISSING"); layers[visibility[v].id].visible = visibility[v].visible; }
                doc.save(); doc.close(SaveOptions.DONOTSAVECHANGES); doc = app.open(new File(q.input));
                write(q.report, {status:'complete', operation:q.operation, before:before, document:snapshot(doc)});
              } else if (q.operation === 'style') {
                if (!/\.renamed\.psd$/i.test(q.input)) throw Error('WORKING_COPY_REQUIRED');
                var lookup = {};
                function collect(parent) { for (var k = 0; k < parent.layers.length; k++) { var l = parent.layers[k]; lookup[String(l.id)] = l; if (l.typename === 'LayerSet') collect(l); } }
                collect(doc); var state = snapshot(doc);
                for (var s = 0; s < q.styles.length; s++) { var style = q.styles[s], layer = lookup[style.id]; if (!layer || layer.name !== style.name) throw Error('STYLE_IDENTITY_CHANGED'); }
                for (var s = 0; s < q.styles.length; s++) { var style = q.styles[s], layer = lookup[style.id]; doc.activeLayer = layer;
                    if (layer.kind === LayerKind.TEXT) layer.textItem.color = color(style.color);
                    else { doc.selection.selectAll(); doc.selection.fill(color(style.color), ColorBlendMode.NORMAL, 100, true); doc.selection.deselect(); }
                }
                for (var s = state.layers.length - 1; s >= 0; s--) lookup[state.layers[s].id].visible = state.layers[s].visible;
                doc.save(); doc.close(SaveOptions.DONOTSAVECHANGES); doc = app.open(new File(q.input));
                write(q.report, {status:'complete', operation:q.operation, document:snapshot(doc)});
            } else if (q.operation === 'preview') {
                var outputs = [];
                for (var p = 0; p < q.previews.length; p++) {
                    var item = q.previews[p], copy = doc.duplicate('Ember S4 preview', false);
                    try {
                        function show(parent) { for (var z = 0; z < parent.layers.length; z++) { var l = parent.layers[z]; l.visible = true; if (l.typename === 'LayerSet') show(l); } }
                        show(copy);
                        for (var z = 0; z < copy.layers.length; z++) copy.layers[z].visible = copy.layers[z].name === item.rootName;
                        function hide(parent) { for (var z = 0; z < parent.layers.length; z++) { var l = parent.layers[z]; if (l.typename === 'LayerSet') hide(l); for (var h = 0; h < item.hiddenNames.length; h++) if (l.name === item.hiddenNames[h]) l.visible = false; } }
                        hide(copy);
                        if (new File(item.output).exists) throw Error('PREVIEW_EXISTS');
                        copy.saveAs(new File(item.output), new PNGSaveOptions(), true, Extension.LOWERCASE);
                        outputs.push(item.output);
                    } finally { copy.close(SaveOptions.DONOTSAVECHANGES); app.activeDocument = doc; }
                }
                write(q.report, {status:'complete', operation:q.operation, outputs:outputs, document:snapshot(doc)});
            } else if (q.operation === 'inspect') write(q.report, {status:'complete', operation:q.operation, document:snapshot(doc)});
            else throw Error('UNKNOWN_OPERATION');
        }
        return 'complete';
    } finally { app.preferences.rulerUnits = previousUnits; app.displayDialogs = previousDialogs; }
})();
