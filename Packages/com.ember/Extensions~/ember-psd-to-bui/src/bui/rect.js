/* global globalThis */

(function registerBuiRect(global) {
  function createBuiRect(options) {
    const { toNumber, round2 } = options;

    function parseBounds(bounds) {
      if (!bounds) {
        return null;
      }

      if (Array.isArray(bounds) && bounds.length >= 4) {
        return {
          left: toNumber(bounds[0], 0),
          top: toNumber(bounds[1], 0),
          right: toNumber(bounds[2], 0),
          bottom: toNumber(bounds[3], 0)
        };
      }

      const left = toNumber(bounds.left, null);
      const top = toNumber(bounds.top, null);
      const right = toNumber(bounds.right, null);
      const bottom = toNumber(bounds.bottom, null);

      if ([left, top, right, bottom].some((value) => value === null || Number.isNaN(value))) {
        return null;
      }

      return { left, top, right, bottom };
    }


    function snapBounds(left, top, right, bottom, clampCanvas) {
      const rawLeft = toNumber(left, 0);
      const rawTop = toNumber(top, 0);
      const rawRight = toNumber(right, rawLeft);
      const rawBottom = toNumber(bottom, rawTop);
      let width = rawRight > rawLeft ? Math.max(1, Math.round(rawRight - rawLeft)) : 0;
      let height = rawBottom > rawTop ? Math.max(1, Math.round(rawBottom - rawTop)) : 0;
      let snappedLeft = Math.round(rawLeft);
      let snappedTop = Math.round(rawTop);
      let snappedRight = snappedLeft + width;
      let snappedBottom = snappedTop + height;

      if (clampCanvas) {
        const canvasWidth = Math.round(toNumber(clampCanvas.width, 0));
        const canvasHeight = Math.round(toNumber(clampCanvas.height, 0));
        if (canvasWidth > 0 && width > 0) {
          snappedLeft = Math.max(0, Math.min(canvasWidth - 1, snappedLeft));
          snappedRight = Math.max(snappedLeft + 1, Math.min(canvasWidth, snappedRight));
          width = snappedRight - snappedLeft;
        }

        if (canvasHeight > 0 && height > 0) {
          snappedTop = Math.max(0, Math.min(canvasHeight - 1, snappedTop));
          snappedBottom = Math.max(snappedTop + 1, Math.min(canvasHeight, snappedBottom));
          height = snappedBottom - snappedTop;
        }
      }

      return {
        left: snappedLeft,
        top: snappedTop,
        right: snappedRight,
        bottom: snappedBottom,
        width,
        height
      };
    }


    function makeRectFromSnappedBounds(bounds, canvas) {
      const canvasWidth = Math.max(1, Math.round(toNumber(canvas && canvas.width, bounds.width)));
      const canvasHeight = Math.max(1, Math.round(toNumber(canvas && canvas.height, bounds.height)));
      const width = bounds.width;
      const height = bounds.height;
      const leftPx = bounds.left;
      const topPx = bounds.top;

      return {
        anchorMin: [0.5, 0.5],
        anchorMax: [0.5, 0.5],
        pivot: [0.5, 0.5],
        anchoredPosition: [
          round2((leftPx + width * 0.5) - canvasWidth * 0.5),
          round2(canvasHeight * 0.5 - (topPx + height * 0.5))
        ],
        sizeDelta: [width, height]
      };
    }


    function makeRect(left, top, right, bottom, canvas) {
      return makeRectFromSnappedBounds(snapBounds(left, top, right, bottom, canvas), canvas);
    }

    return { parseBounds, makeRect, snapBounds, makeRectFromSnappedBounds };
  }

  global.BurnerPsdToBuiRect = { createBuiRect };
})(globalThis);
