# BSSongCoverImageFix
beatsaber mod that resizes song cover images to 256x256 and generates mipmaps.
no configuration, just install the plugin and it should work.

## Why?
* some maps have song cover that are really big (3000x3000 or more!). loading these textures into GPU memory causes the game to stutter (on my system, at least).
  * solution: resize all song cover images to 256x256 (on a background thread, to prevent frame drops)
* the base game loads song cover images using `UnityWebRequestTexture`, which returns textures without [mipmaps](https://en.wikipedia.org/wiki/Mipmap)
  * solution: use `ImageConversion.LoadImage` instead (which is also much faster).

