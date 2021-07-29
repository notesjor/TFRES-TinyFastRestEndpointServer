namespace Tfres
{
  /// <summary>
  ///   A chunk of data, used when reading from a request where the Transfer-Encoding header includes 'chunked'.
  /// </summary>
  public class Chunk
  {
    /// <summary>
    ///   Data.
    /// </summary>
    public byte[] Data { get; set; } = null;

    /// <summary>
    ///   Indicates whether or not this is the final chunk, i.e. the chunk length received was zero.
    /// </summary>
    public bool IsFinalChunk { get; set; } = false;

    /// <summary>
    ///   Length of the data.
    /// </summary>
    public int Length { get; set; } = 0;

    /// <summary>
    ///   Any additional metadata that appears on the length line after the length hex value and semicolon.
    /// </summary>
    public string Metadata { get; set; } = null;

    internal Chunk()
    {
    }
  }
}