using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>Local XY particle billboards on a development-center-authored Image. No extra camera or global canvas.</summary>
    public sealed class NovelParticleMesh : BaseMeshEffect
    {
        #region 内部参数
        private ParticleSystem[] _systems;
        private ParticleSystem.Particle[] _particles = new ParticleSystem.Particle[1024];
        private Vector2 _position, _scale;
        public float Alpha { get; set; } = 1;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public void Configure(ParticleSystem[] systems, Vector2 position, Vector2 scale)
        { _systems = systems; _position = position; _scale = scale; graphic.SetVerticesDirty(); }
        public void Refresh() { if (graphic) graphic.SetVerticesDirty(); }
        public override void ModifyMesh(VertexHelper mesh)
        {
            mesh.Clear();
            if (_systems == null) return;
            var rect = graphic.rectTransform.rect;
            var size = rect.size;
            int vertices = 0;
            foreach (var system in _systems)
            {
                int count = system.GetParticles(_particles);
                for (int i = 0; i < count && vertices < 60000; i++)
                {
                    var particle = _particles[i];
                    Vector3 local = system.transform.localPosition + particle.position;
                    Vector2 center = rect.min + Vector2.Scale(_position + Vector2.Scale((Vector2)local, _scale), size);
                    Vector3 particleSize = particle.GetCurrentSize3D(system);
                    Vector2 half = Vector2.Scale(Vector2.Scale((Vector2)particleSize, _scale), size) * .5f;
                    Color color = particle.GetCurrentColor(system); color.a *= Alpha;
                    float rotation = -particle.rotation * Mathf.Deg2Rad;
                    Vector2 x = new Vector2(Mathf.Cos(rotation), Mathf.Sin(rotation)) * half.x;
                    Vector2 y = new Vector2(-Mathf.Sin(rotation), Mathf.Cos(rotation)) * half.y;
                    mesh.AddVert(center - x - y, color, new Vector2(0, 0));
                    mesh.AddVert(center - x + y, color, new Vector2(0, 1));
                    mesh.AddVert(center + x + y, color, new Vector2(1, 1));
                    mesh.AddVert(center + x - y, color, new Vector2(1, 0));
                    mesh.AddTriangle(vertices, vertices + 1, vertices + 2); mesh.AddTriangle(vertices, vertices + 2, vertices + 3);
                    vertices += 4;
                }
            }
        }
        #endregion
    }
}
