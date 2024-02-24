with import <nixpkgs> {};
  pkgs.mkShell rec {
    dotnetPkg = with dotnetCorePackages;
      combinePackages [
        sdk_8_0
        sdk_7_0
        sdk_6_0
      ];
    dotnetBinary = lib.getExe dotnetPkg;

    deps = [
      zlib
      zlib.dev
      openssl
      dotnetPkg
      icu
    ];

    NIX_LD_LIBRARY_PATH = lib.makeLibraryPath ([stdenv.cc.cc] ++ deps);
    NIX_LD = "${pkgs.stdenv.cc.libc_bin}/bin/ld.so";
    nativeBuildInputs = [graphviz] ++ deps;

    shellHook = ''
      DOTNET_ROOT="${dotnetPkg}";
      MSBuildSdksPath="${dotnetPkg}/${builtins.head dotnetPkg.versions}/Sdks"
      MSBUILD_EXE_PATH="${dotnetPkg}/${builtins.head dotnetPkg.versions}/MSBuild.dll"
      LD_LIBRARY_PATH="$LD_LIBRARY_PATH:${NIX_LD_LIBRARY_PATH}"
    '';
  }
