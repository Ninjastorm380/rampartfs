pkgname=rampartfs
pkgver=1.3.0
pkgrel=1
epoch=1
pkgdesc=""
arch=(x86_64)
url=""
license=('unknown')
groups=()
depends=()
makedepends=(dotnet-host dotnet-runtime dotnet-targeting-pack netstandard-targeting-pack dotnet-sdk git)
checkdepends=()
optdepends=()
provides=()
conflicts=()
replaces=()
backup=()
options=()
install=
changelog=
source=()
noextract=()
sha256sums=()
validpgpkeys=()

prepare() {
	git clone "https://github.com/Ninjastorm380/rampartfs.git"
}

build() {
    cd $srcdir/rampartfs
	dotnet publish $srcdir/rampartfs/rampartfs/rampartfs.csproj -c release -r linux-x64 -p:PublishSingleFile=true -p:PublishReadyToRun=true -p:DebugType=None -p:DebugSymbols=false -p:StripSymbols=true -p:IncludeNativeLibrariesForSelfExtract=true --self-contained -o $srcdir/rampartfs/rampartfs/bin/Publish/net9.0/linux-x64
}

check() {
	return 0
}

package() {
	mkdir $pkgdir/usr
	mkdir $pkgdir/usr/bin

	cp $srcdir/rampartfs/rampartfs/bin/Publish/net9.0/linux-x64/rampartfs $pkgdir/usr/bin/rampartfs
}
