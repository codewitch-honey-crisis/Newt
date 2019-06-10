using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace Grimoire
{
	/// <summary>
	/// Classification type definition export for EbnfClassifier
	/// </summary>
	internal static class EbnfClassifierClassificationDefinition
	{
		// This disables "The field is never used" compiler's warning. Justification: the field is used by MEF.
#pragma warning disable 169

		/// <summary>
		/// Defines the "EbnfClassifier" classification type.
		/// </summary>
		[Export(typeof(ClassificationTypeDefinition))]
		[Name("EbnfClassifier")]
		private static ClassificationTypeDefinition typeDefinition;

		#region Constants
		/// <summary>
		/// The name of the content type for the "Colorful" language.
		/// </summary>
		internal const string ContentType = "Newt.Ebnf";

		/// <summary>
		/// The file extension for files containing the "Colorful" language.
		/// </summary>
		internal const string FileExtension = ".ebnf";
		#endregion // Constants

		#region Managed Extensibility Framework (MEF) Fields
		/// <summary>
		/// The content type definition for the "Colorful" language, which is based on
		/// the pre-defined Visual Studio content type "code".
		/// </summary>
		[Export]
		[Name(ContentType)]
		[BaseDefinition("code")]
		internal static ContentTypeDefinition ContentTypeDefinition = null;

		/// <summary>
		/// The mapping of the ".ebnf" file extension to the content type definition for the "EBNF" language.
		/// </summary>
		[Export]
		[Name(ContentType + nameof(FileExtensionToContentTypeDefinition))]
		[ContentType(ContentType)]
		[FileExtension(FileExtension)]
		internal static FileExtensionToContentTypeDefinition FileExtensionToContentTypeDefinition = null;
		#endregion // Managed Extensibility Framework (MEF) Fields
#pragma warning restore 169
	}
}
