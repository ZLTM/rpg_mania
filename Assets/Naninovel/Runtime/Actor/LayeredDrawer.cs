// Copyright 2023 ReWaffle LLC. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace Naninovel
{
    public class LayeredDrawer : IDisposable
    {
        public IReadOnlyCollection<ILayeredActorLayer> Layers => layers;

        private readonly Transform transform;
        private readonly Material sharedMaterial;
        private readonly Camera camera;
        private readonly bool reversed;
        private readonly List<ILayeredActorLayer> layers = new List<ILayeredActorLayer>();
        private readonly CommandBuffer commandBuffer = new CommandBuffer();
        private readonly MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();

        private RenderCanvas renderCanvas;
        private Rect canvasRect;

        public LayeredDrawer (Transform transform, Camera camera = null, Material sharedMaterial = default, bool reversed = false)
        {
            this.transform = transform;
            this.camera = camera;
            this.sharedMaterial = sharedMaterial;
            this.reversed = reversed;
            commandBuffer.name = $"Naninovel-DrawLayered-{transform.name}";
            BuildLayers();
        }

        public void Dispose () => ClearLayers();

        public void BuildLayers ()
        {
            ClearLayers();
            if (camera) BuildCameraLayers();
            else BuildRendererLayers();
            UpdateCanvas();
        }

        public virtual RenderTexture DrawLayers (float pixelsPerUnit, RenderTexture renderTexture = default)
        {
            if (camera) return DrawWithCamera(pixelsPerUnit, renderTexture);

            if (layers is null || layers.Count == 0)
                throw new Error($"Can't render layered actor `{transform.name}`: layers data is empty. Make sure the actor prefab contains child objects with at least one renderer.");

            var drawDimensions = EvaluateDrawDimensions(pixelsPerUnit);
            var drawPosition = (Vector2)transform.position + canvasRect.position;
            var orthoMin = -drawDimensions / 2f + drawPosition * pixelsPerUnit;
            var orthoMax = drawDimensions / 2f + drawPosition * pixelsPerUnit;
            var orthoMatrix = Matrix4x4.Ortho(orthoMin.x, orthoMax.x, orthoMin.y, orthoMax.y, float.MinValue, float.MaxValue);
            renderTexture = PrepareRenderTexture(renderTexture, drawDimensions);
            PrepareCommandBuffer(renderTexture, orthoMatrix);

            if (reversed)
            {
                for (int i = layers.Count - 1; i >= 0; i--)
                    DrawLayer((RendererLayer)layers[i], pixelsPerUnit);
            }
            else
            {
                for (int i = 0; i < layers.Count; i++)
                    DrawLayer((RendererLayer)layers[i], pixelsPerUnit);
            }

            Graphics.ExecuteCommandBuffer(commandBuffer);

            return renderTexture;
        }

        public void DrawGizmos ()
        {
            if (renderCanvas) return; // Render canvas draws its own gizmo.
            if (!Application.isPlaying)
            {
                if (CountRenderers() != layers.Count) BuildLayers();
                else UpdateCanvas();
            }
            Gizmos.DrawWireCube(transform.position + (Vector3)canvasRect.position, canvasRect.size);
        }

        private Vector2 EvaluateDrawDimensions (float pixelsPerUnit)
        {
            return canvasRect.size * pixelsPerUnit;
        }

        private RenderTexture DrawWithCamera (float pixelsPerUnit, RenderTexture renderTexture = default)
        {
            camera.enabled = false;
            var dimensions = renderCanvas
                ? EvaluateDrawDimensions(pixelsPerUnit)
                : new Vector2(camera.pixelWidth, camera.pixelHeight);
            camera.targetTexture = PrepareRenderTexture(renderTexture, dimensions);
            camera.Render();
            return camera.targetTexture;
        }

        private void ClearLayers ()
        {
            foreach (var layer in layers)
                layer.Dispose();
            layers.Clear();
        }

        private IReadOnlyCollection<Renderer> GetRenderers ()
        {
            return transform.GetComponentsInChildren<Renderer>()
                .OrderBy(s => s.sortingOrder)
                .ThenByDescending(s => s.transform.position.z).ToArray();
        }

        private void UpdateCanvas ()
        {
            if (transform.TryGetComponent<RenderCanvas>(out renderCanvas))
                canvasRect = renderCanvas.Rect;
            else canvasRect = EvaluateCanvasRect();
        }

        private Rect EvaluateCanvasRect ()
        {
            if (layers is null || layers.Count == 0) return Rect.zero;

            float minX = float.PositiveInfinity, minY = float.PositiveInfinity;
            float maxX = float.NegativeInfinity, maxY = float.NegativeInfinity;
            foreach (var layer in layers.OfType<RendererLayer>())
            {
                var min = layer.Renderer.bounds.min - transform.position;
                var max = layer.Renderer.bounds.max - transform.position;
                if (min.x < minX) minX = min.x;
                if (min.y < minY) minY = min.y;
                if (max.x > maxX) maxX = max.x;
                if (max.y > maxY) maxY = max.y;
            }
            var offset = new Vector2(minX + maxX, minY + maxY) / 2;
            var size = new Vector2(maxX - minX, maxY - minY);
            return new Rect(offset, size);
        }

        private RenderTexture PrepareRenderTexture (RenderTexture renderTexture, Vector2 drawDimensions)
        {
            var width = Mathf.RoundToInt(drawDimensions.x);
            var height = Mathf.RoundToInt(drawDimensions.y);
            if (!renderTexture || renderTexture.width != width || renderTexture.height != height)
                return RenderTexture.GetTemporary(width, height);
            return renderTexture;
        }

        private void PrepareCommandBuffer (RenderTexture renderTexture, Matrix4x4 orthoMatrix)
        {
            commandBuffer.Clear();
            commandBuffer.SetRenderTarget(renderTexture);
            commandBuffer.ClearRenderTarget(true, true, Color.clear);
            commandBuffer.SetProjectionMatrix(orthoMatrix);
        }

        private void DrawLayer (RendererLayer layer, float pixelsPerUnit)
        {
            if (!layer.Enabled) return;

            var drawMaterial = sharedMaterial ? sharedMaterial : layer.Renderer.sharedMaterial;
            var drawTransform = Matrix4x4.TRS(layer.Position * pixelsPerUnit, layer.Rotation, layer.Scale * pixelsPerUnit);
            layer.GetPropertyBlock(propertyBlock);
            commandBuffer.DrawMesh(layer.Mesh, drawTransform, drawMaterial, 0, -1, propertyBlock);
        }

        private int CountRenderers ()
        {
            var result = 0;
            CountIn(transform);
            return result;

            void CountIn (Transform trs)
            {
                for (int i = 0; i < trs.childCount; i++)
                {
                    var child = trs.GetChild(i);
                    if (child.TryGetComponent<SpriteRenderer>(out _) ||
                        child.TryGetComponent<MeshFilter>(out _)) result++;
                    CountIn(child);
                }
            }
        }

        private void BuildCameraLayers ()
        {
            var layer = GetFirstEnabledLayer();
            foreach (var child in transform.GetComponentsInChildren<Transform>(true))
                if (child.gameObject.layer == layer)
                    layers.Add(new CameraLayer(child.gameObject));

            int GetFirstEnabledLayer ()
            {
                if (camera.cullingMask == 0)
                    throw new Error($"Make sure at least one layer is enabled in '{transform.gameObject}' layered actor.");
                for (int l = 1; l < 32; l++)
                    if ((camera.cullingMask & (1 << l)) != 0)
                        return l;
                return -1;
            }
        }

        private void BuildRendererLayers ()
        {
            var renderers = GetRenderers();
            if (renderers.Count == 0) return;

            foreach (var renderer in renderers)
            {
                if (renderer is SpriteRenderer spriteRenderer)
                {
                    if (!spriteRenderer.sprite) continue;
                    layers.Add(new RendererLayer(spriteRenderer));
                    continue;
                }

                if (!renderer.TryGetComponent<MeshFilter>(out var meshFilter)) continue;
                layers.Add(new RendererLayer(renderer, meshFilter.sharedMesh ? meshFilter.sharedMesh : meshFilter.mesh));
            }
        }
    }
}
