using DotLiquidCore.Exceptions;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
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