using System.Diagnostics.SymbolStore;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using SimpleCompiler.IR;

namespace SimpleCompiler.Backends.Cil;

internal sealed class EmitContext(ModuleBuilder moduleBuilder, IrGraph irGraph)
{
    // From: https://github.com/dotnet/runtime/blob/main/docs/design/specs/PortablePdb-Metadata.md#embedded-source-c-and-vb-compilers
    private static readonly Guid s_embeddedSourceCodeKind = new("0E8A571B-6926-466E-B4AD-8AB04611F5FE");

    public ISymbolDocumentWriter? SymbolDocumentWriter { get; } = GetSymbolDocumentWriter(moduleBuilder, irGraph);

    public void EmbedSourceCode(MetadataBuilder pdbBuilder)
    {
        if (irGraph.DebugData?.SourceFile != null)
        {
            BlobBuilder sourceBlob = new BlobBuilder();
            sourceBlob.WriteInt32(0); // Raw uncompressed contents
            sourceBlob.WriteBytes(irGraph.DebugData.SourceFile.Encoding.GetBytes(irGraph.DebugData.SourceFile.Contents));
            pdbBuilder.AddCustomDebugInformation(MetadataTokens.DocumentHandle(1),
                pdbBuilder.GetOrAddGuid(s_embeddedSourceCodeKind), pdbBuilder.GetOrAddBlob(sourceBlob));
        }
    }

    public static ISymbolDocumentWriter? GetSymbolDocumentWriter(ModuleBuilder moduleBuilder, IrGraph irGraph)
    {
        if (irGraph.DebugData?.SourceFile != null)
        {
            return moduleBuilder.DefineDocument(
                irGraph.DebugData.SourceFile.Path,
                SymConstants.LanguageGuid,
                SymConstants.VendorGuid,
                SymDocumentType.Text);
        }

        return null;
    }
}
