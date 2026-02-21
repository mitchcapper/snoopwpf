namespace Snoop.Core.Tests.Infrastructure;

using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using NUnit.Framework;
using Snoop.Data.Tree;
using Snoop.Infrastructure;
using VerifyNUnit;

[TestFixture]
public class XMLTreeExporterTests
{
    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        // Assert correct expected formatting
        Assert.That(default(Point).ToString(), Is.EqualTo("0,0"), CultureInfo.CurrentCulture.NativeName);

        // Required to ensure ScrollViewer attached properties are initialized
        // ReSharper disable once UnusedVariable
        var scrollViewer = new ScrollViewer();
    }

    [Test]
    public Task TestTreeWithXamlStyleSimplifiedOpts()
    {
        var options = GetNewStyleOptions(true);
        options.ExportXamlStyle = true;
        return RunTest(new(string.Empty, false), options, true);
    }

    [Test]
    public Task TestTreeWithXamlStyle()
    {
        var options = GetOldStyleOptions(true);
        options.ExportXamlStyle = true;

        return RunTest(new(string.Empty, false), options, true);
    }

    [Test]
    public Task TestTreeWithoutPropertyFilter()
    {
        return RunTest(new(string.Empty, false), GetOldStyleOptions(true));
    }

    [Test]
    public Task TestTreeWithoutPropertyFilterSimplified()
    {
        return RunTest(new(string.Empty, false), GetNewStyleOptions(true));
    }

    private static Task RunTest(PropertyFilter filter, ExportOptions options, bool useDecimals = false)
    {
        var textWriter = new StringWriter();

        var exporter = new XMLTreeExporter();
        exporter.Export(useDecimals ? GetTestTreeItemWithDecimals() : GetTestTreeItem(), textWriter, filter, options);

        var result = textWriter.ToString();

        return Verifier.Verify(result);
    }

    [Test]
    public Task TestTreeWithPropertyFilter()
    {
        return RunTest(new("Height", false), GetOldStyleOptions(true));
    }

    private static ExportOptions GetNewStyleOptions(bool recurse)
    {
        return new ExportOptions() { Recurse = recurse, IncludeDefaultEmptyValues = false, ExportXamlStyle = false, RoundDecimals = true, IncludeTypenameOnlyValues = false, IncludeSystemCollectionNamespaceValues = false };
    }

    private static ExportOptions GetOldStyleOptions(bool recurse)
    {
        return new ExportOptions() { Recurse = recurse, IncludeDefaultEmptyValues = true, ExportXamlStyle = false, RoundDecimals = false, IncludeTypenameOnlyValues = true, IncludeSystemCollectionNamespaceValues = true };
    }

    [Test]
    public Task TestElementWithoutPropertyFilter()
    {
        return RunTest(new(string.Empty, false), GetOldStyleOptions(false));
    }

    [Test]
    public Task TestElementWithPropertyFilter()
    {
        return RunTest(new("Height", false), GetOldStyleOptions(false));
    }

    private static TreeItem GetTestTreeItemWithDecimals()
    {
        var ret = GetTestTreeItem();
        var sp = (StackPanel)ret.Target;
        sp.Width = 123.456789;
        sp.Height = 987.65432;
        sp.Margin = new Thickness(1.23456789, 9.87654321, 0, 0);
        return ret;
    }

    private static TreeItem GetTestTreeItem()
    {
        var target = new StackPanel();
        target.Children.Add(new TextBlock { Text = "test" });
        target.Children.Add(new Border { Child = new CheckBox { Content = "check" } });

        using var treeService = TreeService.From(TreeType.Visual);
        return treeService.Construct(target, null);
    }
}