﻿using System.Text.RegularExpressions;
using AssetsTools.NET;
using AssetsTools.NET.Extra;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.Semantics;
using ICSharpCode.Decompiler.TypeSystem;
using Kaitai;
using Newtonsoft.Json;
using YamlDotNet.Serialization;
using static Kaitai.UnityBundle.BlockInfoAndDirectoryT;

const string OUTPUT_DIR = "zh-hans";

var cliArgs = Environment.GetCommandLineArgs();
if (cliArgs.Length != 2)
{
  Console.Error.WriteLine("Error: game directory is missing, pass game directory as the first argument!");
  Environment.Exit(1);
}

var gameDirectory = cliArgs[1];
if (!Directory.Exists(gameDirectory))
{
  Console.Error.WriteLine($"Invalid game directory: {gameDirectory}!");
  Environment.Exit(1);
}

var managedAssembly = Path.Join(gameDirectory, "The Scroll of Taiwu_Data", "Managed", "Assembly-CSharp.dll");
if (!File.Exists(managedAssembly))
{
  Console.Error.WriteLine($"Invalid managed assembly: {managedAssembly}!");
  Environment.Exit(1);
}

var module = new PEFile(managedAssembly);
var resolver = new UniversalAssemblyResolver(managedAssembly, false, module.DetectTargetFrameworkId());

var settings = new DecompilerSettings(LanguageVersion.Latest)
{
  ThrowOnAssemblyResolveErrors = true,
};
var decompiler = new CSharpDecompiler(managedAssembly, resolver, settings);
var fullTypeName = new FullTypeName("LanguageKey");
var ast = decompiler.DecompileType(fullTypeName);

var typeDeclaration = (TypeDeclaration)ast.Children.First(node => node as TypeDeclaration != null);
var fieldDeclaration = (FieldDeclaration)typeDeclaration.Children.First(node => node is FieldDeclaration && (((FieldDeclaration)node).Modifiers & Modifiers.Private) != 0);
var variableInitializer = (VariableInitializer)fieldDeclaration.Children.First(node => node is VariableInitializer);
var objectCreateExpression = (ObjectCreateExpression)variableInitializer.Children.First(node => node is ObjectCreateExpression);
var arrayInitializer = (ArrayInitializerExpression)objectCreateExpression.Children.First(node => node is ArrayInitializerExpression);

var languageKeyToLineMapping = arrayInitializer.Children.Aggregate(new Dictionary<string, int>(), (acc, node) =>
{
  if (!(node is ArrayInitializerExpression)) throw new Exception("invalid array node");
  var arrayNode = (ArrayInitializerExpression)node;

  if (!(arrayNode.FirstChild is PrimitiveExpression)) throw new Exception("invalid key node");
  var keyNode = (PrimitiveExpression)arrayNode.FirstChild;
  var key = (string)keyNode.Value;

  if (!(arrayNode.LastChild is PrimitiveExpression)) throw new Exception("invalid val node");
  var valNode = (PrimitiveExpression)arrayNode.LastChild;
  var val = (int)valNode.Value;

  acc[key] = val;

  return acc;
});
var eventsDirectory = Path.Join(gameDirectory, "Event", "EventLanguages");
if (!Directory.Exists(eventsDirectory))
{
    Console.Error.WriteLine($"Invalid events directory: {eventsDirectory}!");
    Environment.Exit(1);
}

Console.WriteLine("[+] saving EventLanguages...");

DirectoryInfo d = new DirectoryInfo(eventsDirectory); //Assuming Test is your Folder
Console.WriteLine("Loading files");
Dictionary<string, FileInfo> files = d.GetFiles("*.txt").ToDictionary(file => file.Name); //Getting Text files
Console.WriteLine("Generating Templates");
Dictionary<string, TaiWuTemplate> parsedTemplates = files.ToDictionary(f => f.Key, f => new TaiWuTemplate(f.Value));

Dictionary<string, string> flatDict = parsedTemplates.Values
    .ToList()
    .SelectMany(template => template.FlattenTemplateToDict())
    .ToDictionary(pair => pair.Key, pair => pair.Value);
File.WriteAllText(Path.Join(OUTPUT_DIR, "events.json"), JsonConvert.SerializeObject(flatDict, Formatting.Indented));

var languageCnAssetBundle = Path.Join(gameDirectory, "The Scroll of Taiwu_Data", "GameResources", "language_cn.uab");
if (!File.Exists(languageCnAssetBundle))
{
  Console.Error.WriteLine($"Invalid language_cn.uab: {languageCnAssetBundle}!");
  Environment.Exit(1);
}



    var manager = new AssetsManager();

    var bunInst = manager.LoadBundleFile(languageCnAssetBundle, true);



        var afileInst = manager.LoadAssetsFileFromBundle(bunInst, 0, true);

        var afile = afileInst.file;
        foreach (var texInfo in afile.GetAssetsOfType(AssetClassID.TextAsset))
        {

            if (!Directory.Exists(OUTPUT_DIR))
            {
                Directory.CreateDirectory(OUTPUT_DIR);
            }

            var texBase = manager.GetBaseField(afileInst, texInfo);
            var name = texBase["m_Name"].AsString;
            var text = texBase["m_Script"].AsString;
            Console.WriteLine($"[+] saving {name}...");

    

    if (name == "ui_language")
    {
        var lines = text.Split("\n");
        var entries = languageKeyToLineMapping.Aggregate(new Dictionary<string, string>(), (acc, pair) =>
        {
            acc[pair.Key] = lines[pair.Value];

            return acc;
        });
        File.WriteAllText(Path.Join(OUTPUT_DIR, $"{name}.json"), JsonConvert.SerializeObject(entries, Formatting.Indented));

    }
    
    if (name == "Adventure_language")
    {
        int v = 0;
        //We're trying to avoid empty lines, so we'll count lines until the ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>" separator [before : txt, after, json] and setting it as v. Then, we iterate through the txt with v as an upper limit.
        var lines = text.Split("\n");
        var jsonlines = lines.Where(x => x.Contains("LK_") && x.Contains("="));
        var dict = new Dictionary<string, string>();
        foreach (var l in jsonlines)
        {
            if (l.Contains("="))
            {
                dict.Add(l.Split("=")[0], l.Split("=")[1]);
            }
        }

        File.WriteAllText(Path.Join(OUTPUT_DIR, $"{name}.json"), JsonConvert.SerializeObject(dict, Formatting.Indented));
        v = lines.Count();
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < v; i++)
        {
            if (lines[i] == ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>")
            {
                Console.WriteLine("line n°" + i + ": " + lines[i]);
                sb.Append(lines[i].ToString());
                v = i;
                break;
            }

            if (!jsonlines.Contains(lines[i]))
            {

                    sb.Append(lines[i].ToString() + "\n");
                Console.WriteLine("line n°" + i + ": " + lines[i]);
            }
        }

        File.WriteAllText(Path.Join(OUTPUT_DIR, $"{name}.txt"), sb.ToString());


    }
    if(name != "Adventure_language" && name != "ui_language")
    { 
    File.WriteAllText(Path.Join(OUTPUT_DIR, $"{name}.txt"), text);
    }
}









