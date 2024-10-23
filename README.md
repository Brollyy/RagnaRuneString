# RagnaRuneString

Inspired by [Hearthstone Deckstrings](https://hearthsim.info/docs/deckstrings), rune strings allow for encoding and decoding lists of Ragnarock runes into a string format.

Rune strings are intended to provide a consistent data format for copying and sharing rune patterns.

## Supported by

- RagnaRuneString Discord bot (WIP) will render a preview of a small rune pattern from a rune string when mentioned on [RagnaCustoms](https://discord.gg/H6UeS5uH) server.
- Map editor [Edda by PKBeam](https://github.com/PKBeam/Edda) supports copying and pasting notes in RagnaRuneString format since v1.2.7 release.

## Usage

Use `RuneStringSerializer` class methods `Serialize` and `Deserialize` methods to convert between rune strings and their underlying data, which depends on the version (see [Format structure](#format-structure) for more info).

|Version|Data format|
|---|---|
|1|`Version1.RagnaStringData`|

In case only a single version of the format needs to be supported, a more specific `DeserializeV*` method can be used to skip a runtime check on the deserialized output.

## Format structure

The rune string is base64-encoded compact byte string. Varint in this context means 7-bit encoded int as defined by .NET Framework `BinaryWriter` and `BinaryReader`.

### 1. Header block

1. Reserved byte `0x00`.
2. Version byte (`1`).

The rune string begins with the byte `0x00`. It is then followed by the encoding version number. The version is currently always 1.

### Version 1

Below sections describe the format of the data included after the header block in version 1.

#### Runes block

Runes are encoded in three sections depending on the number of runes in the same row, encoded in the following order:

1. Single rune section.
2. Double rune section.
3. N runes in a row section.

Each section begins with a leading varint specifying the number of rows in that section. Row timing is encoded as varint of the original rune time multiplied by 1024 and rounded down - this is a lossy transformation, although the error is unnoticeable for all intents and purposes.

##### Single rune section

Single rune section consists of two arrays: first is the array of each row timing, followed by `ceil(rows/8)` bytes encoding columns (line indices in BeatSaber map format) for all single runes.
Since there are 4 possible columns to place runes on, each column is encoded in 2 bits and the array is densely packed, with the last byte padded with zeros as needed.

Example with `-` indicating major gridline and `*` indicating a rune:
```
---*   <-- time = 1.0
  * 
 *  
*   
----   <-- time = 0.0
```
This pattern gets encoded to single note section in hex as
`04__8002_8004_8006_8008__1B`, with the first byte `04` encoding the count of 4 single runes, `8002`, `8004`, `8006` and `8008` varints encoding the row timings `0.25`, `0.5`, `0.75` and `1.0`, and finally the byte `1B` (or in binary `00_01_10_11`) encoding column values `0`, `1`, `2` and `3`.

##### Double rune section

Double rune section consists of two arrays as well: first is the array of row timings as before, followed by `3 * ceil(ceil(rows/8)/3)` bytes encoding columns for the double runes.
There are 6 different combinations of columns you can place a double rune on:

|Double rune columns|Combination value|
|---|---|
|`0, 1`|`0`|
|`0, 2`|`1`|
|`0, 3`|`2`|
|`1, 2`|`3`|
|`1, 3`|`4`|
|`2, 3`|`5`|

Combination values for double runes are encoded in 3 bits and the array is densely packed, writing 8 combination values into 3 bytes at a time, padding the last 3 bytes with zeros as needed.

Example with `-` indicating major gridline and `*` indicating a rune:
```
-*-*   <-- time = 1.0
* * 
 ** 
*  *
----   <-- time = 0.0
```
This pattern gets encoded to double note section in hex as
`04__8002_8004_8006_8008__4CC000`, with the first byte `04` encoding the count of 4 double runes, `8002`, `8004`, `8006` and `8008` varints encoding the row timings `0.25`, `0.5`, `0.75` and `1.0`, and finally 3 bytes `4C C0 00` (or in binary `010_011_001_100__000000000000`) encoding column combination values `2`, `3`, `1` and `4`, followed by 12 bits of padding.

##### N-rune section

Due to n-rune rows not being particularly common in Ragnarock, no compression is applied to this section. It consists of an array of items, each one being described in order by:

1. Varint encoding row timing.
2. Byte specifying number of runes in a row.
3. Array of bytes encoding rune column values (one byte per column value).

Single and double runes can also be potentially described in this section, but it's recommended to include them in their respective sections for better compression, which is also how the `RuneStringSerializer.Serialize` behaves.

Note that this format can describe 5 or more runes in a row, with possibly duplicate column values - it's up to the user to decide how to interpret these runes, although there should be no problems ignoring them. `RuneStringSerializer.Serialize` only includes 3 and 4 rune rows in this section.

### BPM changes block

BPM changes are encoded as an array, starting with a varint specifying number of BPM changes, followed by repeated sections describing single items:

1. Starting time (in global beat time) of the BPM change encoded in the same way as rune time.
2. BPM value encoded in the same way as rune time.

Although BPM changes are not required to describe timing of runes, they might be useful to consumers for context (e.g. visualization of patterns), so it's still recommended to include them in the rune string.
Only include BPM changes that are relevant to the encoded runes.

Global BPM for a rune string can be specified as a BPM change with start time equal to 0.