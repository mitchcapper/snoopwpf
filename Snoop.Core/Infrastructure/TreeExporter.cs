namespace Snoop.Infrastructure;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Xml;
using Snoop.Data.Tree;

public static class TreeExporter
{
    public static void Export(TreeItem treeItem, TextWriter textWriter, PropertyFilter? filter, bool recurse = true)
    {
        new XMLTreeExporter().Export(treeItem, textWriter, filter, recurse);
    }
}

public class ExportOptions : DependencyObject
{
    public static readonly DependencyProperty TreeItemProperty = DependencyProperty.Register(
        nameof(TreeItem), typeof(TreeItem), typeof(ExportOptions), new PropertyMetadata(default(TreeItem)));

    public TreeItem? TreeItem
    {
        get { return (TreeItem?)this.GetValue(TreeItemProperty); }
        set { this.SetValue(TreeItemProperty, value); }
    }

    public static readonly DependencyProperty UseFilterProperty = DependencyProperty.Register(
        nameof(UseFilter), typeof(bool), typeof(ExportOptions), new PropertyMetadata(true));

    public bool UseFilter
    {
        get { return (bool)this.GetValue(UseFilterProperty); }
        set { this.SetValue(UseFilterProperty, value); }
    }

    public static readonly DependencyProperty RecurseProperty = DependencyProperty.Register(
        nameof(Recurse), typeof(bool), typeof(ExportOptions), new PropertyMetadata(false));

    public bool Recurse
    {
        get { return (bool)this.GetValue(RecurseProperty); }
        set { this.SetValue(RecurseProperty, value); }
    }

    public bool ExportXamlStyle { get; set; }

    public bool IncludeDefaultEmptyValues { get; set; }

    public bool IncludeTypenameOnlyValues { get; set; }

    public bool IncludeSystemCollectionNamespaceValues { get; set; }

    public bool RoundDecimals { get; set; }
}

public class XMLTreeExporter
{
    private static readonly Dictionary<Type, object?> defaultValueCache = new();

    private static object? GetDefaultValue(Type type)
    {
        if (!defaultValueCache.TryGetValue(type, out var defaultValue))
        {
            defaultValue = Activator.CreateInstance(type);
            defaultValueCache[type] = defaultValue;
        }

        return defaultValue;
    }

    public void Export(TreeItem treeItem, TextWriter textWriter, PropertyFilter? filter, bool recurse = true)
    {
        var options = new ExportOptions
        {
            Recurse = recurse,
            ExportXamlStyle = true,
            RoundDecimals = true,
            IncludeDefaultEmptyValues = false
        };
        this.Export(treeItem, textWriter, filter, options);
    }

    public void Export(TreeItem treeItem, TextWriter textWriter, PropertyFilter? filter, ExportOptions options)
    {
        var writerSettings = new XmlWriterSettings
        {
            Encoding = textWriter.Encoding,
            Indent = true,
            NewLineOnAttributes = false
        };

        using var xmlWriter = XmlWriter.Create(textWriter, writerSettings);
        xmlWriter.WriteStartDocument(true);
        this.ExportItem(treeItem, xmlWriter, filter, options);
        xmlWriter.WriteEndDocument();
    }

    private void ExportItem(TreeItem treeItem, XmlWriter xmlWriter, PropertyFilter? filter, ExportOptions options)
    {
        if (!options.ExportXamlStyle)
    {
        xmlWriter.WriteStartElement("node");
        xmlWriter.WriteAttributeString("name", treeItem.Name);
        xmlWriter.WriteAttributeString("displayName", treeItem.DisplayName);
        xmlWriter.WriteAttributeString("targetType", treeItem.TargetType.FullName!);
        }
        else
        {
            var elementName = GetValidXmlName(treeItem.TargetType.Name);
            xmlWriter.WriteStartElement(elementName);
        }

        var propertyInformations = PropertyInformation.GetProperties(treeItem.Target);

        if (propertyInformations.Any())
        {
            if (!options.ExportXamlStyle)
        {
            xmlWriter.WriteStartElement("properties");
            }

            foreach (var propertyInformation in propertyInformations)
            {
                if (filter is not null
                    && filter.ShouldShow(propertyInformation) == false)
                {
                    continue;
                }

                var rawVal = propertyInformation.Value;
                if (options.IncludeDefaultEmptyValues == false)
                {
                    var skipValue = false;
                    skipValue |= rawVal is null;
                    skipValue |= rawVal is string strVal && string.IsNullOrEmpty(strVal);
                    if (!skipValue && propertyInformation.PropertyType.Type.IsValueType == true)
                    {
                        var defaultVal = GetDefaultValue(rawVal!.GetType());
                        if (rawVal.Equals(defaultVal))
                        {
                            skipValue = true;
                        }
                    }

                    if (!skipValue && !options.IncludeSystemCollectionNamespaceValues)
                    {
                        var typeNamespace = propertyInformation.PropertyType.Type.Namespace ?? string.Empty;
                        if (typeNamespace.StartsWith("System.Collections", StringComparison.Ordinal))
                        {
                            skipValue = true;
                        }
                    }

                    if (skipValue)
                    {
                        propertyInformation.Teardown();
                        continue;
                    }
                }

                if (rawVal == null)
                {
                    rawVal = "null";
                }

                string? value;
                if (options.RoundDecimals)
                {
                    value = rawVal switch
                    {
                        double d => d.ToString("0.#"),
                        float f => f.ToString("0.#"),
                        decimal m => m.ToString("0.#"),
                        _ => rawVal.ToString()
                    };
                }
                else
                {
                    value = rawVal.ToString()!;
                }

                if (options.IncludeTypenameOnlyValues == false)
                {
#pragma warning disable CA1307 // Specify StringComparison for clarity
                    if (value!.Contains('.') && value == propertyInformation.PropertyType.Type.FullName)
                    {
                        propertyInformation.Teardown();
                        continue;
                    }
#pragma warning restore CA1307 // Specify StringComparison for clarity
                }

                if (!options.ExportXamlStyle)
                {
                xmlWriter.WriteStartElement("property");
                xmlWriter.WriteAttributeString("displayName", propertyInformation.DisplayName);
                    xmlWriter.WriteAttributeString("value", value);
                xmlWriter.WriteEndElement();
                }
                else
                {
                    var attributeName = GetValidXmlName(propertyInformation.DisplayName);
                    xmlWriter.WriteAttributeString(attributeName, value);
                }

                propertyInformation.Teardown();
            }

            if (!options.ExportXamlStyle)
            {
            xmlWriter.WriteEndElement();
        }
        }

        if (options.Recurse)
        {
            foreach (var treeItemChild in treeItem.Children)
            {
                this.ExportItem(treeItemChild, xmlWriter, filter, options);
            }
        }

        xmlWriter.WriteEndElement();
    }

    private static string GetValidXmlName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return "Element";
        }

        return XmlConvert.EncodeLocalName(name);
    }
}