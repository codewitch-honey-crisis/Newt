using System.ComponentModel.Composition;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace Grimoire
{
	/// <summary>
	/// Defines an editor format for the EbnfClassifier type that has a purple background
	/// and is underlined.
	/// </summary>
	[Export(typeof(EditorFormatDefinition))]
	[ClassificationType(ClassificationTypeNames = "EbnfClassifier")]
	[Name("EbnfClassifier")]
	[UserVisible(true)] // This should be visible to the end user
	[Order(Before = Priority.Default)] // Set the priority to be after the default classifiers
	internal sealed class EbnfClassifierFormat : ClassificationFormatDefinition
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="EbnfClassifierFormat"/> class.
		/// </summary>
		public EbnfClassifierFormat()
		{
			this.DisplayName = "EbnfClassifier"; // Human readable version of the name
			this.BackgroundColor = Colors.BlueViolet;
			this.TextDecorations = System.Windows.TextDecorations.Underline;
		}
	}
}
