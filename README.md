# bzPSD

A .NET Standard 2.1 library for reading and writing Adobe Photoshop PSD files, written entirely in managed C#.

## Installation

Add the project reference directly, or copy the source into your solution. A NuGet package is not yet published.

## Requirements

- .NET Standard 2.1 or later (.NET 5+, .NET Core 3.x)
- `System.Drawing.Common` 5.0.3+

## Quick start

### Load a PSD and decode the merged image

```csharp
var psd = new PsdFile().Load("photo.psd");
Bitmap merged = ImageDecoder.DecodeImage(psd);
```

### Composite layers (respects blend modes and opacity)

```csharp
var psd = new PsdFile().Load("layered.psd");
Bitmap composited = ImageDecoder.CompositeLayers(psd);
```

### Decode a single layer

```csharp
var psd = new PsdFile().Load("layered.psd");
foreach (Layer layer in psd.Layers)
{
    if (!layer.Visible) continue;
    Bitmap bmp = ImageDecoder.DecodeImage(layer);   // null if layer is empty
    // bmp is sized to layer.Rect, positioned at layer.Rect.X / .Y
}
```

### Load from a byte array or stream

```csharp
byte[] bytes = File.ReadAllBytes("photo.psd");
var psd = new PsdFile().Load(bytes);

// or
using var stream = File.OpenRead("photo.psd");
var psd = new PsdFile().Load(stream);
```

### Save a PSD

```csharp
psd.Save("output.psd");

// or to a stream
using var stream = File.Create("output.psd");
psd.Save(stream);
```

### Create a PSD from scratch

```csharp
// Allocates zero-filled channel data ready for you to populate
PsdFile psd = PsdFile.Create(800, 600, ColorMode.RGB, depth: 8);

// Fill channel data (channels are stored as separate planar arrays)
// psd.ImageData[0] = red channel, [1] = green, [2] = blue
for (int i = 0; i < psd.Rows * psd.Columns; i++)
{
    psd.ImageData[0][i] = 255; // solid red
    psd.ImageData[1][i] = 0;
    psd.ImageData[2][i] = 0;
}

psd.Save("red.psd");
```

## API reference

### `PsdFile`

| Member | Description |
|--------|-------------|
| `Load(string)` | Load from a file path. Returns `this` for chaining. |
| `Load(byte[])` | Load from a byte array. |
| `Load(Stream)` | Load from a stream. |
| `Save(string)` | Save to a file path. |
| `Save(Stream)` | Write to a stream. |
| `PsdFile.Create(width, height, colorMode, depth)` | Factory: blank PSD with allocated image data. |
| `Rows`, `Columns` | Image dimensions in pixels (1–30 000). |
| `Channels` | Number of channels including alpha (1–24). |
| `Depth` | Bits per channel: 1, 8, or 16. |
| `ColorMode` | `RGB`, `CMYK`, `Grayscale`, `Lab`, `Indexed`, etc. |
| `ImageData` | `byte[][]` — one array per channel, planar order. |
| `ImageCompression` | `Raw` or `Rle`. |
| `ColorModeData` | 768-byte palette for Indexed; duotone data otherwise. |
| `Layers` | `IEnumerable<Layer>` — ordered bottom-to-top. |
| `ImageResources` | `IEnumerable<ImageResource>` — metadata blocks. |
| `Resolution` | `ResolutionInfo` resource (DPI, units). |
| `DisplayInfo` | Per-channel display info (resource 1007): colour space, ink colour, opacity, kind. |
| `AlphaChannelNames` | ASCII extra-channel names (resource 1006). |
| `UnicodeAlphaNames` | Unicode extra-channel names (resource 1045, preferred over `AlphaChannelNames`). |
| `AddLayer(Layer)` | Append a layer to the stack. |
| `RemoveLayer(Layer)` | Remove a layer from the stack. |

### `ImageDecoder`

| Method | Description |
|--------|-------------|
| `DecodeImage(PsdFile)` | Decode the pre-flattened merged image stored in the file. |
| `CompositeLayers(PsdFile)` | Flatten all visible layers bottom-to-top using blend modes and opacity. |
| `DecodeImage(Layer)` | Decode a single layer's pixel data. Returns `null` for zero-size layers. The bitmap is sized to `layer.Rect`. |
| `DecodeImage(Layer.Mask)` | Decode a layer mask as a greyscale bitmap. |

Supported blend modes: Normal, Multiply, Screen, Overlay, Hard Light, Soft Light, Darken, Lighten, Difference, Exclusion, Color Dodge, Color Burn. Unrecognised keys fall through to Normal.

### `Layer`

| Member | Description |
|--------|-------------|
| `Name` | Layer name. |
| `Rect` | Bounding rectangle in canvas coordinates. |
| `Visible` | Whether the layer is visible. |
| `Opacity` | 0 (transparent) – 255 (opaque). |
| `BlendModeKey` | Four-character Photoshop blend mode key, e.g. `"norm"`, `"mul "`. |
| `Clipping` | `true` = clipped to the layer below. |
| `Channels` | Raw channel list. |
| `SortedChannels` | Channels keyed by ID: 0=R, 1=G, 2=B, -1=transparency, -2=mask. |
| `MaskData` | Layer mask (`Layer.Mask`). |
| `AdjustmentInfo` | Additional layer data blocks (raw `AdjusmentLayerInfo` list). |
| `IsTextLayer` | `true` when the layer has a TySh (Type Sheet) block. |
| `TextData` | `TextLayer` accessor, or `null` if not a text layer. Cached per layer instance. |

### Text layers

```csharp
var psd = new PsdFile().Load("template.psd");

foreach (Layer layer in psd.Layers)
{
    if (!layer.IsTextLayer) continue;

    Console.WriteLine($"{layer.Name}: "{layer.TextData.Text}"");

    // Update the text; all formatting is preserved.
    layer.TextData.Text = "Hello, world!";
}

psd.Save("output.psd");
```

### `TextLayer`

| Member | Description |
|--------|-------------|
| `Text` | The text string. Getting reads the `Txt ` key from the descriptor; setting rewrites the TySh block in memory. |
| `Transform` | Copy of the 6-element affine transform `[xx, xy, yx, yy, tx, ty]`. `tx`/`ty` give the canvas position. |

### Color modes

| `ColorMode` | Channels decoded | Notes |
|-------------|-----------------|-------|
| `RGB` | 3 + optional alpha | |
| `CMYK` | 4 | Converted to RGB on decode. |
| `Grayscale` / `Duotone` | 1 | Rendered as grey. |
| `Lab` | 3 | Converted to RGB on decode. |
| `Indexed` | 1 | Palette resolved via `ColorModeData`. |
| `Multichannel` | all channels | Each ink composited subtractively on white using `DisplayInfo` (resource 1007) colours. Falls back to C/M/Y/K if `DisplayInfo` is absent. |

### Bit depth

8-bit and 16-bit per channel are fully supported. For 16-bit files the high byte of each channel word is used for display (values are stored big-endian). 1-bit files are parsed but not decoded to colour.

## Limitations

- PSD only (version 1). PSB (large document, version 2) is not supported.
- Writing layer pixel data round-trips correctly only when `ImageData` on each channel has been populated. Newly constructed layers need their channel `ImageData` set before saving.
- `Dissolve` blend mode is not implemented (falls back to Normal).
- `System.Drawing.Common` is a Windows-native GDI+ wrapper on non-Windows platforms and may require additional runtime configuration.

## License

BSD 3-Clause — see [LICENSE](LICENSE).
