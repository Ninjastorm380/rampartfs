pkgname=rampartfs
pkgver=1.3.0
pkgrel=3
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
options=(!strip)
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
	dotnet publish $srcdir/rampartfs -c release
}

check() {
	return 0
}

package() {
	mkdir $pkgdir/usr
	mkdir $pkgdir/usr/bin
	mkdir $pkgdir/usr/lib

	cp $srcdir/rampartfs/rampartfs/bin/Release/net9.0/linux-x64/publish/rampartfs $pkgdir/usr/bin/rampartfs
	cp $srcdir/rampartfs/rampartfs/bin/Release/net9.0/linux-x64/publish/libMonoFuseHelper.so $pkgdir/usr/lib/libMonoFuseHelper.so
	cp $srcdir/rampartfs/rampartfs/bin/Release/net9.0/linux-x64/publish/libMonoPosixHelper.so $pkgdir/usr/lib/libMonoPosixHelper.so
}