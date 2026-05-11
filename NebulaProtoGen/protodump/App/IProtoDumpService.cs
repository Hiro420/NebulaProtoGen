using google.protobuf;

namespace ProtoDescDumper.App;

public interface IProtoDumpService
{
	int Run(FileDescriptorSet set, string outputDir);
}
