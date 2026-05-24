using System.Collections.Generic;
using UnityEngine;

namespace TwelveMoons.City
{
    public static class CityBuildingOutlineRuntime
    {
        private static readonly Dictionary<CityBuildingOutlineEffect, OutlineRequest> Requests =
            new Dictionary<CityBuildingOutlineEffect, OutlineRequest>();

        public static bool HasActiveOutline => Requests.Count > 0;

        public static Color CurrentColor { get; private set; } = new Color(1f, 0.62f, 0.12f, 1f);

        public static int CurrentPixelWidth { get; private set; } = 3;

        public static void Register(
            CityBuildingOutlineEffect owner,
            Renderer[] renderers,
            Color color,
            int pixelWidth)
        {
            if (owner == null)
            {
                return;
            }

            Requests[owner] = new OutlineRequest(renderers, color, Mathf.Max(1, pixelWidth));
            RefreshCurrentStyle();
        }

        public static void Unregister(CityBuildingOutlineEffect owner)
        {
            if (owner == null)
            {
                return;
            }

            Requests.Remove(owner);
            RefreshCurrentStyle();
        }

        public static void CollectActiveRenderers(List<Renderer> target)
        {
            target.Clear();
            foreach (var request in Requests.Values)
            {
                if (request.Renderers == null)
                {
                    continue;
                }

                foreach (var renderer in request.Renderers)
                {
                    if (renderer != null &&
                        renderer.enabled &&
                        renderer.gameObject.activeInHierarchy &&
                        !target.Contains(renderer))
                    {
                        target.Add(renderer);
                    }
                }
            }
        }

        private static void RefreshCurrentStyle()
        {
            foreach (var request in Requests.Values)
            {
                CurrentColor = request.Color;
                CurrentPixelWidth = request.PixelWidth;
                return;
            }

            CurrentColor = new Color(1f, 0.62f, 0.12f, 1f);
            CurrentPixelWidth = 3;
        }

        private readonly struct OutlineRequest
        {
            public OutlineRequest(Renderer[] renderers, Color color, int pixelWidth)
            {
                Renderers = renderers;
                Color = color;
                PixelWidth = pixelWidth;
            }

            public Renderer[] Renderers { get; }

            public Color Color { get; }

            public int PixelWidth { get; }
        }
    }
}
