using HarmonyLib;
using System;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using System.Drawing;

namespace BSSongCoverImageFix
{
    [HarmonyPatch(typeof(CachedMediaAsyncLoader), nameof(CachedMediaAsyncLoader.LoadSpriteAsync))]
    internal class LoadSpriteAsyncPatch
    {
        [HarmonyPrefix]
        static void Prefix(
            int ____maxNumberOfSpriteCachedElements,
            ref AsyncCachedLoader<string, Sprite> ____spriteAsyncCachedLoader
        )
        {
            if (____spriteAsyncCachedLoader == null)
            {
                ____spriteAsyncCachedLoader = new AsyncCachedLoader<string, Sprite>(
                    ____maxNumberOfSpriteCachedElements,
                    MediaAsyncLoader_LoadSpriteAsync_replacement
                );
                Plugin.Log.Info("replaced LoadSpriteAsync");
            }
        }

        public static async Task<Sprite> MediaAsyncLoader_LoadSpriteAsync_replacement(
            string path,
            CancellationToken cancellationToken
        )
        {
            if (!File.Exists(path))
            {
                // fall back to original method if `path` isn't a local file that exists
                var sw = new Stopwatch();
                sw.Start();
                var sp = await MediaAsyncLoader.LoadSpriteAsync(path, cancellationToken);
                var elapsed = sw.Elapsed.TotalMilliseconds;
                if (elapsed > 8.0)
                {
                    if (sp != null)
                    {
                        Plugin.Log.Warn(
                            $"async sprite took {elapsed} ms to load: {sp.texture.width}x{sp.texture.height} {path}"
                        );
                    }
                    else
                    {
                        Plugin.Log.Warn(
                            $"async sprite took {elapsed} ms to load, and returned null {path}"
                        );
                    }
                }
                return sp;
            }

            var spriteTexture = await ReadAndResizeImageAsync(path, cancellationToken);
            if (cancellationToken.IsCancellationRequested)
            {
                return null;
            }

            float pixelsPerUnit = 100.0f;
            return spriteTexture != null
                ? Sprite.Create(
                    spriteTexture,
                    new Rect(0, 0, spriteTexture.width, spriteTexture.height),
                    new Vector2(0, 0),
                    pixelsPerUnit
                )
                : null;
        }

        private static async Task<Texture2D> ReadAndResizeImageAsync(
            string path,
            CancellationToken cancellationToken
        )
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return null;
            }
            byte[] file = await Task.Run(() =>
            {
                var sw1 = new Stopwatch();
                sw1.Start();
                var originalImage = Image.FromFile(path);
                if (cancellationToken.IsCancellationRequested)
                {
                    return null;
                }
                Bitmap resizedImage = new Bitmap(originalImage, 256, 256);
                if (cancellationToken.IsCancellationRequested)
                {
                    return null;
                }
                using var anotherMemoryStreamYippee = new MemoryStream();
                resizedImage.Save(anotherMemoryStreamYippee, originalImage.RawFormat);
                if (cancellationToken.IsCancellationRequested)
                {
                    return null;
                }

                var elapsed1 = sw1.Elapsed.TotalMilliseconds;
                if (elapsed1 > 50.0)
                {
                    Plugin.Log.Info($"read and resize took {elapsed1} ms: {path}");
                }
                return anotherMemoryStreamYippee.ToArray();
            });
            if (file == null)
            {
                return null;
            }
            if (cancellationToken.IsCancellationRequested)
            {
                return null;
            }

            var sw2 = new Stopwatch();
            sw2.Start();
            Texture2D spriteTexture = new Texture2D(256, 256);
            if (!spriteTexture.LoadImage(file))
                return null;
            var elapsed2 = sw2.Elapsed.TotalMilliseconds;
            if (elapsed2 > 8.0 && spriteTexture != null)
            {
                Plugin.Log.Warn(
                    $"LoadImage took {elapsed2} ms: {spriteTexture.width}x{spriteTexture.height} {path}"
                );
            }
            return spriteTexture;
        }
    }
}
