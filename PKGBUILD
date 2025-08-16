# This is an example PKGBUILD file. Use this as a start to creating your own,
# and remove these comments. For more information, see 'man PKGBUILD'.
# NOTE: Please fill out the license field for your package! If it is unknown,
# then please put 'unknown'.

pkgname=rampartfs
pkgver=1.0.2
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
	dotnet publish . -c release -r linux-x64 -o $srcdir/RampartFS/bin/Publish/net9.0/linux-x64
}

check() {
	return 0
}

package() {
	mkdir $pkgdir/usr
	mkdir $pkgdir/usr/bin
	mkdir $pkgdir/usr/lib

	cp $srcdir/RampartFS/bin/Publish/net9.0/linux-x64/rampartfs $pkgdir/usr/bin/rampartfs
	cp $srcdir/RampartFS/bin/Publish/net9.0/linux-x64/libMono.Unix.so $pkgdir/usr/lib/libMono.Unix.so
}
