// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

namespace SimpleCompiler.Backend.Cil.Emit
{
    internal sealed class PersistedAssemblyBuilder : AssemblyBuilder
    {
        private readonly AssemblyName _assemblyName;
        private readonly Assembly _coreAssembly;
        private readonly MetadataBuilder _metadataBuilder;
        private ModuleBuilderImpl? _module;
        private bool _previouslySaved;

        internal List<CustomAttributeWrapper>? _customAttributes;

        internal PersistedAssemblyBuilder(AssemblyName name, Assembly coreAssembly, IEnumerable<CustomAttributeBuilder>? assemblyAttributes = null)
        {
            _assemblyName = (AssemblyName) name.Clone();
            _coreAssembly = coreAssembly;
            _metadataBuilder = new MetadataBuilder();

            if (assemblyAttributes != null)
            {
                foreach (CustomAttributeBuilder assemblyAttribute in assemblyAttributes)
                {
                    SetCustomAttribute(assemblyAttribute);
                }
            }
        }

        private void WritePEImage(Stream peStream, BlobBuilder ilBuilder, BlobBuilder fieldData)
        {
            var peHeaderBuilder = new PEHeaderBuilder(
                // For now only support DLL, DLL files are considered executable files
                // for almost all purposes, although they cannot be directly run.
                imageCharacteristics: Characteristics.ExecutableImage | Characteristics.Dll);

            var peBuilder = new ManagedPEBuilder(
                header: peHeaderBuilder,
                metadataRootBuilder: new MetadataRootBuilder(_metadataBuilder),
                ilStream: ilBuilder,
                mappedFieldData: fieldData,
                strongNameSignatureSize: 0);

            // Write executable into the specified stream.
            var peBlob = new BlobBuilder();
            peBuilder.Serialize(peBlob);
            peBlob.WriteContentTo(peStream);
        }

        /// <summary>
        /// Serializes the assembly to <see cref="Stream"/>.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> to which the assembly serialized.</param>
        /// <exception cref="ArgumentNullException"><paramref name="stream"/> is null.</exception>
        /// <exception cref="NotSupportedException">The AssemblyBuilder instance doesn't support saving.</exception>
        public void Save(Stream stream)
        {
            ArgumentNullException.ThrowIfNull(stream);
            GenerateMetadata(out var ilStream, out var mappedFieldData);
            WritePEImage(stream, ilStream, mappedFieldData);
        }

        /// <summary>
        /// Saves the assembly to disk.
        /// </summary>
        /// <param name="assemblyFileName">The file name of the assembly.</param>
        /// <exception cref="ArgumentNullException"><paramref name="assemblyFileName"/> is null.</exception>
        /// <exception cref="NotSupportedException">The AssemblyBuilder instance doesn't support saving.</exception>
        public void Save(string assemblyFileName)
        {
            ArgumentNullException.ThrowIfNull(assemblyFileName);

            using var peStream = new FileStream(assemblyFileName, FileMode.Create, FileAccess.Write);
            Save(peStream);
        }

        public MetadataBuilder GenerateMetadata(out BlobBuilder ilStream, out BlobBuilder mappedFieldData)
        {
            if (_module == null)
            {
                throw new InvalidOperationException("TODO: Fill in with result of SR.InvalidOperation_AModuleRequired");
            }

            if (_previouslySaved) // Cannot save an assembly multiple times. This is consistent with Save() in .Net Framework.
            {
                throw new InvalidOperationException("TODO: Fill in with result of SR.InvalidOperation_CannotSaveMultipleTimes");
            }

            // Add assembly metadata
            AssemblyDefinitionHandle assemblyHandle = _metadataBuilder.AddAssembly(
               _metadataBuilder.GetOrAddString(value: _assemblyName.Name!),
               version: _assemblyName.Version ?? new Version(0, 0, 0, 0),
               culture: _assemblyName.CultureName == null ? default : _metadataBuilder.GetOrAddString(value: _assemblyName.CultureName),
               publicKey: _assemblyName.GetPublicKey() is byte[] publicKey ? _metadataBuilder.GetOrAddBlob(value: publicKey) : default,
               flags: AddContentType((AssemblyFlags) _assemblyName.Flags, _assemblyName.ContentType),
#pragma warning disable SYSLIB0037 // Type or member is obsolete
               hashAlgorithm: (AssemblyHashAlgorithm) _assemblyName.HashAlgorithm
#pragma warning restore SYSLIB0037
               );
            _module.WriteCustomAttributes(_customAttributes, assemblyHandle);

            ilStream = new BlobBuilder();
            mappedFieldData = new BlobBuilder();
            MethodBodyStreamEncoder methodBodyEncoder = new MethodBodyStreamEncoder(ilStream);
            _module.AppendMetadata(methodBodyEncoder, mappedFieldData);
            _previouslySaved = true;
            return _metadataBuilder;
        }

        private static AssemblyFlags AddContentType(AssemblyFlags flags, AssemblyContentType contentType)
            => (AssemblyFlags) ((int) contentType << 9) | flags;

        protected override ModuleBuilder DefineDynamicModuleCore(string name)
        {
            if (_module != null)
            {
                throw new InvalidOperationException("TODO: Fill in with result of SR.InvalidOperation_NoMultiModuleAssembly");
            }

            _module = new ModuleBuilderImpl(name, _coreAssembly, _metadataBuilder, this);
            return _module;
        }

        protected override ModuleBuilder? GetDynamicModuleCore(string name)
        {
            if (_module != null && _module.ScopeName.Equals(name))
            {
                return _module;
            }

            return null;
        }

        protected override void SetCustomAttributeCore(ConstructorInfo con, ReadOnlySpan<byte> binaryAttribute)
        {
            _customAttributes ??= new List<CustomAttributeWrapper>();
            _customAttributes.Add(new CustomAttributeWrapper(con, binaryAttribute));
        }

        public override string? FullName => _assemblyName.FullName;

        public override Module ManifestModule => _module ?? throw new InvalidOperationException("TODO: Fill in with result of SR.InvalidOperation_AModuleRequired");

        public override AssemblyName GetName(bool copiedName) => (AssemblyName) _assemblyName.Clone();
    }
}
