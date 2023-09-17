// Copyright 2023 ReWaffle LLC. All rights reserved.

using System;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Layer inside <see cref="LayeredActorBehaviour"/> object.
    /// </summary>
    public interface ILayeredActorLayer : IDisposable
    {
        string Name { get; }
        string Group { get; }
        bool Enabled { get; set; }
    }

    public class RendererLayer : ILayeredActorLayer
    {
        public string Name { get; }
        public string Group { get; }
        public bool Enabled { get => Renderer.enabled; set => Renderer.enabled = value; }
        public readonly Mesh Mesh;
        public readonly Renderer Renderer;
        public Vector2 Position => Renderer.transform.position;
        public Quaternion Rotation => Renderer.transform.rotation;
        public Vector2 Scale => Renderer.transform.lossyScale;

        private static readonly int spriteColorId = Shader.PropertyToID("_RendererColor");

        private readonly SpriteRenderer spriteRenderer;

        public RendererLayer (Renderer renderer, Mesh mesh)
        {
            Name = renderer.gameObject.name;
            Group = LayeredUtilities.BuildGroupName(renderer.transform);
            Mesh = mesh;
            Renderer = renderer;

            if (Application.isPlaying)
                renderer.forceRenderingOff = true;
        }

        public RendererLayer (SpriteRenderer spriteRenderer) :
            this(spriteRenderer, BuildSpriteMesh(spriteRenderer))
        {
            this.spriteRenderer = spriteRenderer;
        }

        public void Dispose ()
        {
            if (Mesh && Mesh.hideFlags == HideFlags.HideAndDontSave)
                ObjectUtils.DestroyOrImmediate(Mesh);
        }

        public MaterialPropertyBlock GetPropertyBlock (MaterialPropertyBlock block)
        {
            Renderer.GetPropertyBlock(block);
            if (spriteRenderer)
                block.SetColor(spriteColorId, spriteRenderer.color);
            return block;
        }

        private static Mesh BuildSpriteMesh (SpriteRenderer spriteRenderer)
        {
            var sprite = spriteRenderer.sprite;
            var mesh = new Mesh();
            mesh.hideFlags = HideFlags.HideAndDontSave;
            mesh.name = $"{sprite.name} Sprite Mesh";
            mesh.vertices = Array.ConvertAll(sprite.vertices, i => new Vector3(i.x * (spriteRenderer.flipX ? -1 : 1), i.y * (spriteRenderer.flipY ? -1 : 1)));
            mesh.uv = sprite.uv;
            mesh.triangles = Array.ConvertAll(sprite.triangles, i => (int)i);
            return mesh;
        }
    }

    public class CameraLayer : ILayeredActorLayer
    {
        public string Name { get; }
        public string Group { get; }
        public bool Enabled { get => go.activeSelf; set => go.SetActive(value); }

        private readonly GameObject go;

        public CameraLayer (GameObject go)
        {
            this.go = go;
            Name = go.name;
            Group = LayeredUtilities.BuildGroupName(go.transform);
        }

        public void Dispose () { }
    }
}
