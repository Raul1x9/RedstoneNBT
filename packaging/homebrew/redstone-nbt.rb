class RedstoneNbt < Formula
  desc "Modern cross-platform Minecraft NBT & World Editor revived from NBTExplorer"
  homepage "https://github.com/Raul1x9/RedstoneNBT"
  version "1.0.0"
  license "GPL-3.0-or-later"

  if OS.mac?
    if Hardware::CPU.arm?
      url "https://github.com/Raul1x9/RedstoneNBT/releases/download/v1.0.0/redstone-nbt-1.0.0-osx-arm64.tar.gz"
      sha256 "edbecadf10a9a9ef5f0f8a35edc3ff7e1199472d448a80fd77e657b3b482d8c8"
    else
      url "https://github.com/Raul1x9/RedstoneNBT/releases/download/v1.0.0/redstone-nbt-1.0.0-osx-x64.tar.gz"
      sha256 "6725c65adadb9f0767d49648328f12f2f312ec481d65a5716baba7f041952b9d"
    end
  elsif OS.linux?
    url "https://github.com/Raul1x9/RedstoneNBT/releases/download/v1.0.0/redstone-nbt-1.0.0-linux-x64.tar.gz"
    sha256 "bf8ea746376febe49e2017924e81f3348d0447c631eb526eb4ea440575475443"
  end

  def install
    libexec.install Dir["*"]
    bin.install_symlink libexec/"redstone-nbt"
  end

  test do
    assert_predicate bin/"redstone-nbt", :exist?
  end
end
