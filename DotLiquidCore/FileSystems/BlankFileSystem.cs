using DotLiquidCore.Exceptions;

namespace DotLiquidCore.FileSystems
{
	public class BlankFileSystem : IFileSystem
	{
		public string ReadTemplateFile(Context context, string templateName)
		{
			throw new FileSystemException(Liquid.ResourceManager.GetString("BlankFileSystemDoesNotAllowIncludesException"));
		}
	}
}