using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CucumberClicker.EditorTools
{
    /// <summary>
    /// Draws simple placeholder sprites as PNGs so the game runs without any art.
    /// Replace any PNG in the output folder with real art and the scene picks it up.
    /// </summary>
    public static class PlaceholderArt
    {
        public class Set
        {
            public Sprite rounded, circle, shadow, gradient, cucumber, coin, gear, jar, market, arrow, worker;
        }

        public const int RoundedBorder = 48; // matches the drawer's 48 px corner radius

        public static Set Generate(string folder)
        {
            Directory.CreateDirectory(folder);
            var set = new Set
            {
                rounded = Make(folder, "rounded", 128, 128, RoundedRect(128, 128, RoundedBorder, Color.white), RoundedBorder),
                circle = Make(folder, "circle", 128, 128, Circle(128, Color.white)),
                shadow = Make(folder, "shadow", 192, 192, SoftShadow(192, 48, 40), 96),
                gradient = Make(folder, "background", 8, 512, Gradient(8, 512,
                    new Color(0.99f, 0.98f, 0.86f), new Color(0.78f, 0.91f, 0.62f))),
                cucumber = Make(folder, "cucumber", 512, 512, Cucumber(512), readable: true),
                coin = Make(folder, "coin", 128, 128, Coin(128)),
                gear = Make(folder, "gear", 128, 128, Gear(128, Color.white)),
                jar = Make(folder, "icon_jar", 128, 128, Jar(128, Color.white)),
                market = Make(folder, "icon_market", 128, 128, MarketStall(128, Color.white)),
                arrow = Make(folder, "icon_upgrade", 128, 128, Arrow(128, Color.white)),
                worker = Make(folder, "icon_worker", 128, 128, Worker(128, Color.white)),
            };
            return set;
        }

        // ---------- asset plumbing ----------

        private static Sprite Make(string folder, string name, int w, int h, Color[] pixels, int border = 0, bool readable = false)
        {
            string path = $"{folder}/{name}.png";

            // Never overwrite art someone has dropped in.
            if (!File.Exists(path))
            {
                var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                tex.SetPixels(pixels);
                tex.Apply();
                File.WriteAllBytes(path, tex.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path);
            }

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.isReadable = readable || importer.isReadable;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.wrapMode = TextureWrapMode.Clamp;
            if (border > 0) importer.spriteBorder = new Vector4(border, border, border, border);
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // ---------- drawing helpers (signed distance fields, anti-aliased) ----------

        private static Color[] Blank(int w, int h) => new Color[w * h];

        /// <summary>Alpha-blends <paramref name="color"/> wherever sdf(x, y) &lt; 0.</summary>
        private static void Paint(Color[] px, int w, int h, Func<float, float, float> sdf, Func<float, float, Color> color)
        {
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float fx = x + 0.5f, fy = y + 0.5f;
                float coverage = Mathf.Clamp01(0.5f - sdf(fx, fy));
                if (coverage <= 0) continue;

                Color src = color(fx, fy);
                src.a *= coverage;
                Color dst = px[y * w + x];
                float a = src.a + dst.a * (1 - src.a);
                Color rgb = a > 0 ? (src * src.a + dst * dst.a * (1 - src.a)) / a : Color.clear;
                rgb.a = a;
                px[y * w + x] = rgb;
            }
        }

        private static void Paint(Color[] px, int w, int h, Func<float, float, float> sdf, Color color) =>
            Paint(px, w, h, sdf, (_, _) => color);

        private static float CircleSdf(float x, float y, float cx, float cy, float r) =>
            Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) - r;

        private static float BoxSdf(float x, float y, float cx, float cy, float hw, float hh, float r)
        {
            float qx = Mathf.Abs(x - cx) - hw + r, qy = Mathf.Abs(y - cy) - hh + r;
            float outside = new Vector2(Mathf.Max(qx, 0), Mathf.Max(qy, 0)).magnitude;
            return outside + Mathf.Min(Mathf.Max(qx, qy), 0) - r;
        }

        private static float SegmentSdf(float x, float y, Vector2 a, Vector2 b, float r)
        {
            Vector2 p = new Vector2(x, y), pa = p - a, ba = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba));
            return (pa - ba * t).magnitude - r;
        }

        // ---------- shapes ----------

        private static Color[] RoundedRect(int w, int h, float radius, Color color)
        {
            var px = Blank(w, h);
            Paint(px, w, h, (x, y) => BoxSdf(x, y, w / 2f, h / 2f, w / 2f, h / 2f, radius), color);
            return px;
        }

        private static Color[] Circle(int size, Color color)
        {
            var px = Blank(size, size);
            Paint(px, size, size, (x, y) => CircleSdf(x, y, size / 2f, size / 2f, size / 2f - 1), color);
            return px;
        }

        private static Color[] SoftShadow(int size, float radius, float blur)
        {
            var px = Blank(size, size);
            float half = size / 2f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = BoxSdf(x + 0.5f, y + 0.5f, half, half, half - blur, half - blur, radius);
                float a = 1 - Mathf.SmoothStep(0, 1, Mathf.Clamp01((d + blur * 0.15f) / blur));
                px[y * size + x] = new Color(0, 0, 0, a);
            }
            return px;
        }

        private static Color[] Gradient(int w, int h, Color top, Color bottom)
        {
            var px = Blank(w, h);
            for (int y = 0; y < h; y++)
            {
                Color c = Color.Lerp(bottom, top, y / (h - 1f));
                for (int x = 0; x < w; x++) px[y * w + x] = c;
            }
            return px;
        }

        private static Color[] Cucumber(int size)
        {
            var px = Blank(size, size);
            var a = new Vector2(size * 0.30f, size * 0.20f);
            var b = new Vector2(size * 0.70f, size * 0.80f);
            float r = size * 0.19f;
            Vector2 axis = (b - a).normalized, normal = new Vector2(-axis.y, axis.x);

            var outline = new Color(0.17f, 0.42f, 0.13f);
            var dark = new Color(0.25f, 0.56f, 0.18f);
            var light = new Color(0.50f, 0.79f, 0.33f);

            // Stem
            Vector2 stemBase = b + axis * (r * 0.8f);
            Paint(px, size, size, (x, y) => SegmentSdf(x, y, stemBase, stemBase + axis * (size * 0.07f), size * 0.025f),
                new Color(0.42f, 0.33f, 0.16f));

            // Outline then body, shaded across its width
            Paint(px, size, size, (x, y) => SegmentSdf(x, y, a, b, r), outline);
            Paint(px, size, size, (x, y) => SegmentSdf(x, y, a, b, r - size * 0.022f), (x, y) =>
            {
                float side = Vector2.Dot(new Vector2(x, y) - a, normal) / r; // -1 .. 1 across the body
                return Color.Lerp(light, dark, Mathf.Clamp01(0.5f - side * 0.6f));
            });

            // Highlight stripe
            Vector2 hlOffset = normal * (r * 0.45f);
            Paint(px, size, size, (x, y) => SegmentSdf(x, y, a + hlOffset + axis * r * 0.4f, b + hlOffset - axis * r * 0.6f, r * 0.12f),
                new Color(0.78f, 0.93f, 0.6f, 0.75f));

            // Bumps
            var rng = new System.Random(7);
            for (int i = 0; i < 16; i++)
            {
                float t = (float)rng.NextDouble();
                float s = (float)rng.NextDouble() * 1.4f - 0.7f;
                Vector2 c = Vector2.Lerp(a, b, t) + normal * (s * r);
                Paint(px, size, size, (x, y) => CircleSdf(x, y, c.x, c.y, size * 0.012f), new Color(0.82f, 0.95f, 0.62f));
            }

            // Face, so the cucumber has some charm
            Vector2 face = Vector2.Lerp(a, b, 0.58f);
            Vector2 eyeL = face + normal * (r * 0.32f), eyeR = face - normal * (r * 0.32f);
            foreach (Vector2 eye in new[] { eyeL, eyeR })
            {
                Paint(px, size, size, (x, y) => CircleSdf(x, y, eye.x, eye.y, size * 0.034f), Color.white);
                Vector2 pupil = eye - axis * (size * 0.006f);
                Paint(px, size, size, (x, y) => CircleSdf(x, y, pupil.x, pupil.y, size * 0.02f), new Color(0.1f, 0.15f, 0.08f));
            }
            Vector2 mouth = face - axis * (r * 0.5f);
            Paint(px, size, size, (x, y) =>
            {
                float d = Mathf.Abs(CircleSdf(x, y, mouth.x, mouth.y, size * 0.05f)) - size * 0.008f;
                float below = Vector2.Dot(new Vector2(x, y) - mouth, axis); // keep only the lower half: a smile
                return below > 0 ? Mathf.Max(d, below) : d;
            }, new Color(0.1f, 0.25f, 0.08f));

            return px;
        }

        private static Color[] Coin(int size)
        {
            var px = Blank(size, size);
            float c = size / 2f;
            Paint(px, size, size, (x, y) => CircleSdf(x, y, c, c, c - 2), new Color(0.8f, 0.55f, 0.08f));
            Paint(px, size, size, (x, y) => CircleSdf(x, y, c, c, c - 9), new Color(0.99f, 0.78f, 0.2f));
            Paint(px, size, size, (x, y) => Mathf.Abs(CircleSdf(x, y, c, c, c * 0.62f)) - 3, new Color(0.86f, 0.62f, 0.1f));
            Paint(px, size, size, (x, y) => CircleSdf(x, y, c - c * 0.35f, c + c * 0.35f, c * 0.14f), new Color(1, 1, 1, 0.6f));
            return px;
        }

        private static Color[] Gear(int size, Color color)
        {
            var px = Blank(size, size);
            float c = size / 2f;
            Paint(px, size, size, (x, y) =>
            {
                float dx = x - c, dy = y - c;
                float angle = Mathf.Atan2(dy, dx);
                float teeth = Mathf.Clamp01(Mathf.Cos(angle * 8) * 3 + 0.5f);
                float radius = Mathf.Lerp(c * 0.68f, c * 0.92f, teeth);
                float outer = Mathf.Sqrt(dx * dx + dy * dy) - radius;
                float hole = c * 0.3f - Mathf.Sqrt(dx * dx + dy * dy);
                return Mathf.Max(outer, hole);
            }, color);
            return px;
        }

        private static Color[] Jar(int size, Color color)
        {
            var px = Blank(size, size);
            float c = size / 2f;
            Paint(px, size, size, (x, y) => BoxSdf(x, y, c, size * 0.42f, size * 0.32f, size * 0.36f, size * 0.12f), color);
            Paint(px, size, size, (x, y) => BoxSdf(x, y, c, size * 0.86f, size * 0.26f, size * 0.07f, size * 0.03f), color);
            return px;
        }

        private static Color[] MarketStall(int size, Color color)
        {
            var px = Blank(size, size);
            float c = size / 2f;
            // Awning with scalloped edge, posts, counter
            Paint(px, size, size, (x, y) => BoxSdf(x, y, c, size * 0.78f, size * 0.44f, size * 0.12f, size * 0.04f), color);
            for (int i = 0; i < 4; i++)
            {
                float cx = size * (0.17f + i * 0.22f);
                Paint(px, size, size, (x, y) => CircleSdf(x, y, cx, size * 0.66f, size * 0.11f), color);
            }
            Paint(px, size, size, (x, y) => BoxSdf(x, y, size * 0.14f, size * 0.36f, size * 0.04f, size * 0.26f, 2), color);
            Paint(px, size, size, (x, y) => BoxSdf(x, y, size * 0.86f, size * 0.36f, size * 0.04f, size * 0.26f, 2), color);
            Paint(px, size, size, (x, y) => BoxSdf(x, y, c, size * 0.2f, size * 0.44f, size * 0.1f, size * 0.03f), color);
            return px;
        }

        private static Color[] Arrow(int size, Color color)
        {
            var px = Blank(size, size);
            float c = size / 2f;
            Paint(px, size, size, (x, y) => BoxSdf(x, y, c, size * 0.32f, size * 0.12f, size * 0.24f, size * 0.03f), color);
            Paint(px, size, size, (x, y) =>
            {
                // Triangle pointing up: apex at top, base at mid height
                float top = size * 0.92f, baseY = size * 0.52f, halfWidth = size * 0.4f;
                float t = Mathf.InverseLerp(baseY, top, y);
                float edge = Mathf.Abs(x - c) - halfWidth * (1 - t);
                return Mathf.Max(edge, Mathf.Max(baseY - y, y - top));
            }, color);
            return px;
        }

        private static Color[] Worker(int size, Color color)
        {
            var px = Blank(size, size);
            float c = size / 2f;
            Paint(px, size, size, (x, y) => CircleSdf(x, y, c, size * 0.68f, size * 0.2f), color);
            Paint(px, size, size, (x, y) => Mathf.Max(CircleSdf(x, y, c, size * 0.1f, size * 0.38f), size * 0.08f - y), color);
            // Straw hat brim
            Paint(px, size, size, (x, y) => BoxSdf(x, y, c, size * 0.84f, size * 0.36f, size * 0.04f, size * 0.03f), color);
            return px;
        }
    }
}
