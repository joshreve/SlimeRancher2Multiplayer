#pragma warning disable RCS1110 // Declare type inside namespace
// ReSharper disable once CheckNamespace

internal static class BuildInfo
{
    internal const string ID = "joshreve.ranchingparty.core";
    internal const string Name = "Ranching Party";
    internal const string Description = "Adds Multiplayer to Slime Rancher 2. Major rewrite of Ranching Together!";
    internal const string Author = "joshreve";
    internal static readonly string[] CoAuthors = new[] { "Shark" };
    internal static readonly string[] Contributors = new[] { "AlchlcSystm, PinkTarr" };
    // MelonVersion is shown by ML on startup
    // Version is shown by Starlight
    // Version automatically gets a -dev at the end if SR2MP is compiled by GitHub Action
    internal const string MelonVersion = "0.3.3";
    internal const string Version = "0.4.1"; // Auto-Dev_Do_not_remove
    internal const string Discord = "None";
    internal const string SourceCode = "https://github.com/joshreve/SlimeRancher2Multiplayer";
    internal const string Nexus = "None";
    internal const bool UsePrism = false;
    internal const string MinimumStarlightVersion = Starlight.BuildInfo.CodeVersion; // e.g "3.6.3", the min required SR2 version. No beta or alpha versions
    internal const string MinimumGameVersion = "1.2.3"; // e.g 1.1.0 or something similar (optional)
    internal const string ExactGameVersion = "1.2.3"; // e.g 1.1.0 or something similar (optional)
}