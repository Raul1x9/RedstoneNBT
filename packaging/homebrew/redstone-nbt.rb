class RedstoneNbt < Formula
  desc "Modern cross-platform Minecraft NBT & World Editor revived from NBTExplorer"
  homepage "https://github.com/Raul1x9/RedstoneNBT"
  version "1.0.0"
  license "GPL-3.0-or-later"

  if OS.mac?
    if Hardware::CPU.arm?
      url "https://github.com/Raul1x9/RedstoneNBT/releases/download/v1.0.0/redstone-nbt-1.0.0-osx-arm64.tar.gz"
      sha256 "a13aa2cbc4cf49aaabc1308c3d14ddbc0196b5ca7b24d6626f1110635c9d5aa4"
    else
      url "https://github.com/Raul1x9/RedstoneNBT/releases/download/v1.0.0/redstone-nbt-1.0.0-osx-x64.tar.gz"
      sha256 "ece4b4243cd53a4e890282354090d5f9fe6fbcff859fe75835392ebaf342afa0"
    end
  elsif OS.linux?
    url "https://github.com/Raul1x9/RedstoneNBT/releases/download/v1.0.0/redstone-nbt-1.0.0-linux-x64.tar.gz"
    sha256 "aab2326f0b7f32d1ca4f81ab8c1d4c879228970842083ea013d4f325f0890e95"
  end

  def install
    libexec.install Dir["*"]
    bin.install_symlink libexec/"redstone-nbt"
  end

  test do
    assert_predicate bin/"redstone-nbt", :exist?
  end
end
