# This is an example PKGBUILD file. Use this as a start to creating your own,
# and remove these comments. For more information, see 'man PKGBUILD'.
# NOTE: Please fill out the license field for your package! If it is unknown,
# then please put 'unknown'.

# Maintainer: Your Name <youremail@domain.com>
pkgname=rampartfs
pkgver=1
pkgrel=1
epoch=1
pkgdesc=""
arch=(x86_64)
url=""
license=('unknown')
groups=()
depends=()
makedepends=(dotnet-runtime-bin xz tar)
checkdepends=()
optdepends=()
provides=()
conflicts=()
replaces=()
backup=()
options=()
install=
changelog=
source=("https://github.com/Ninjastorm380/rampartfs.git")
noextract=()
sha256sums=('91f5f0b8f3906a4ff0ee22303f0db3a47696bad0f393381534de6249cc27f956')
validpgpkeys=()

prepare() {
	return 0
}

build() {
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
