#addin nuget:?package=Cake.Paket
#tool nuget:?package=Paket
#tool nuget:?package=NUnit.ConsoleRunner&version=3.4.0
#tool nuget:?package=WiX.Toolset
#addin nuget:?package=Cake.VersionReader
#addin nuget:?package=Cake.WinSCP

using WinSCP;

#load "local.cake"
//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////
var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");
//////////////////////////////////////////////////////////////////////
// PREPARATION
//////////////////////////////////////////////////////////////////////
// Define directories.
var buildDir = Directory("./TLCGen/bin") + Directory(configuration);
var setupBuildDir = Directory("./TLCGen.Setup/bin") + Directory(configuration);
var setupDir = Directory("./TLCGen.Setup");
var outputDir = Directory("./published") + Directory(configuration);
//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////
Task("Clean")
    .Does(() =>
{
    CleanDirectory(buildDir);
    CleanDirectory(outputDir);
});

Task("Restore")
    .IsDependentOn("Clean")
	.Does(() =>
{
	PaketRestore();
});

Task("Build")
    .IsDependentOn("Restore")
    .Does(() =>
{
    // Use MSBuild
    MSBuild("./TLCGen.sln", settings =>
      settings.SetConfiguration(configuration));
});

Task("Sign")
	.IsDependentOn("Build")
	.ContinueOnError()
    .Does(() =>
{
    // Use MSBuild
    var file = buildDir + new FilePath("TLCGen.exe");
	Sign(file, new SignToolSignSettings {
            TimeStampUri = certUri,
            CertPath = certPath,
            Password = signPass
    });
});

Task("BuildSetup")
	.IsDependentOn("Sign")
	.Does(() =>
{
	WiXCandle("./TLCGen.Setup/Product.wxs", new CandleSettings
	{
		Defines = new Dictionary<string, string>
		{
			{ "Configuration", configuration }
		},
		OutputDirectory = setupBuildDir,
		WorkingDirectory = setupDir
	});
	WiXLight(setupBuildDir + File("Product.wixobj"), new LightSettings
	{
		WorkingDirectory = setupDir,
		Extensions = new[] { "WixUIExtension" },
		OutputFile = outputDir + File("TLCGen.Setup.msi")
	});
});

Task("SignSetup")
	.IsDependentOn("BuildSetup")
	.ContinueOnError()
    .Does(() =>
{
    var file = outputDir + new FilePath("TLCGen.Setup.msi");
	Sign(file, new SignToolSignSettings {
            TimeStampUri = certUri,
            CertPath = certPath,
            Password = signPass
    });
});

Task("PackPortable")
	.IsDependentOn("SignSetup")
    .Does(() =>
{
    var packedDir = buildDir + new DirectoryPath("packed");
	if (DirectoryExists(packedDir))
	{
	    DeleteDirectory(packedDir, new DeleteDirectorySettings {
			Recursive = true,
			Force = true
		});
	}
	CreateDirectory(packedDir);
	CreateDirectory(packedDir + new DirectoryPath("Libs"));
	CreateDirectory(packedDir + new DirectoryPath("Deps"));
	var version = GetFullVersionNumber(buildDir.Path + "/TLCGen.exe");
	var files = GetFiles(buildDir.Path + "/TLCGen.exe*");
	CopyFiles(files, packedDir);
	
	files = GetFiles(buildDir.Path + "/TLCGen*.dll*");
	CopyFiles(files, packedDir + new DirectoryPath("Libs"));
	
	files = GetFiles(buildDir.Path + "/*.dll*");
	CopyFiles(files, packedDir + new DirectoryPath("Deps"));
	files = GetFiles(packedDir.Path + "/Deps/TLCGen*.dll*");
	DeleteFiles(files);
	files = GetFiles(packedDir.Path + "/Deps/Xceed.Wpf.AvalonDock*.dll");
	DeleteFiles(files);
	files = GetFiles(packedDir.Path + "/Deps/Xceed.Wpf.DataGrid.dll");
	DeleteFiles(files);
	CopyDirectory(buildDir.Path + "/Settings", packedDir.Path + "/Settings");
	CopyDirectory(buildDir.Path + "/SourceFiles", packedDir.Path + "/SourceFiles");
	CopyDirectory(buildDir.Path + "/SourceFilesToCopy", packedDir.Path + "/SourceFilesToCopy");
	CopyDirectory(buildDir.Path + "/Updater", packedDir.Path + "/Updater");
	
	if (!DirectoryExists(buildDir.Path + "/TLCGen_v" + version))
		MoveDirectory(packedDir, buildDir.Path + "/TLCGen_v" + version);

	if (FileExists(outputDir.Path + "/TLCGen_portable_latest.zip"))
	{
		files = GetFiles(outputDir.Path + "/TLCGen_portable_latest.zip");
		DeleteFiles(files);
	}
	Zip(buildDir.Path + "/TLCGen_v" + version, outputDir.Path + "/TLCGen_portable_latest.zip");
});

Task("Deploy")
	.IsDependentOn("PackPortable")
    .Does(() => {
        Information("Starting FTP upload...");
        // Setup session options
        var sessionOptions = new SessionOptions {
                Protocol = Protocol.Sftp,
				HostName = ftpHostname,
				UserName = ftpUser,
				Password = ftpPassword,
				PortNumber = ftpPort,
                SshHostKeyFingerprint = sshFingerPrint
            };
         using (Session session = new Session()) {
                // Setting executable Path
                var winScpExe = File("./Tools/Addins/Cake.WinSCP.0.4.3/lib/netstandard2.0/WinSCP.exe");
                // Connect
                session.Open(sessionOptions);
 		 
                // Upload files
                TransferOptions transferOptions = new TransferOptions();
                transferOptions.TransferMode = TransferMode.Binary;
 		 
				var upFile = File("TLCGen_portable_latest.zip");
                TransferOperationResult transferResult;
                transferResult = session.PutFiles(
				  outputDir.Path.MakeAbsolute(Context.Environment).ToString().Replace("/", "\\"), 
				  "/var/www/codingconnected.eu/wordpress/tlcgen/deploy/", false, transferOptions);
 		 
                // Throw on any error
                transferResult.Check();
 		 
                // Print results
                foreach (TransferEventArgs transfer in transferResult.Transfers) {
                    Information("Upload of {0} succeeded", transfer.FileName);
                }
          }
});
//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////
Task("Default")
    .IsDependentOn("Deploy");
//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////
RunTarget(target);