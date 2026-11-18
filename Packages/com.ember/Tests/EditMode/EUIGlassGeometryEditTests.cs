using NUnit.Framework;
using UnityEngine;

namespace Ember.UI.Tests
{
    public sealed class EUIGlassGeometryEditTests
    {
        [Test]
        public void RoundedRaycast_ClampsRadiusAndRejectsCornerOutsideCoverage()
        {
            var root = new GameObject("Geometry", typeof(RectTransform), typeof(EUIBackdropGraphic));
            try
            {
                var graphic = root.GetComponent<EUIBackdropGraphic>();
                graphic.rectTransform.sizeDelta = new Vector2(100, 100); graphic.Radius = 1000;
                Assert.That(graphic.IsRaycastLocationValid(new Vector2(0, 0), null), Is.True);
                Assert.That(graphic.IsRaycastLocationValid(new Vector2(49, 49), null), Is.False);
                graphic.SquareTop=true;
                Assert.That(graphic.IsRaycastLocationValid(new Vector2(49,49),null),Is.True);
                Assert.That(graphic.IsRaycastLocationValid(new Vector2(49,-49),null),Is.False);
                graphic.SquareTop=false;
                graphic.Radius = float.NaN;
                Assert.That(graphic.Radius, Is.EqualTo(0));
                Assert.That(graphic.IsRaycastLocationValid(new Vector2(49, 49), null), Is.True);
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
