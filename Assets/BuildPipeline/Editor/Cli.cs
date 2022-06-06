using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using CommandLine;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.Build.Reporting;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline;
using UnityEditor.Build.Pipeline.Injector;


namespace AriaSDK.UnityPlayerBuildPipeline
{
    public sealed class Cli
    {
        /// <summary>
        ///
        /// </summary>
        public static void BatchMain()
        {
            string[] args = Environment.GetCommandLineArgs();

            // FormatToUnityCommandlineArguments
            for (int i = 0; i < args.Length; i++)
            {
                args[i] = args[i].Replace("-", "--");
            }

            Debug.Log($"hoge: {args.Length}");
            foreach (var arg in args)
            {
                Debug.Log($"arg: {arg}");
            }
            //CommandLine.Parser.Default.FormatCommandLine()
            var opts = Parser.Default.ParseArguments<Options>(args) as Parsed<Options>;
            if (opts == null)
            {
                Debug.LogError("invalid commandline arguments!!");
                return;
            }

            Debug.Log(opts.Value.InputFile);
        }

        [MenuItem("Tools/ParseTest")]
        private static void ParseTest()
        {
            string[] args = new string[]
            {
                "/Applications/Unity/Hub/Editor/2020.3.33f1/Unity.app/Contents/MacOS/Unity",
                "-input",
                "hoge.txt",
                "-output",
                "huga.xml",
                "-executeMethod",
                "AriaSDK.UnityPlayerBuildPipeline.CLi.BatchMain",
                "-buildTarget",
                "ios",
                "-projectPath",
                "/Users/z.kazuki.tanaka/workspace/tmp/unity-portable-buildpipeline",
                "-nographics",
                "-batchmode",
                "-logfile",
                "-quit"
            };

            for (int i = 0; i < args.Length; i++)
            {
                args[i] = args[i].Replace("-", "--");
            }

            Debug.Log($"hoge: {args.Length}");
            foreach (var arg in args)
            {
                Debug.Log($"arg: {arg}");
            }

            //CommandLine.Parser.Default.FormatCommandLine()
            var opts = Parser.Default.ParseArguments<Options>(args) as Parsed<Options>;
            if (opts == null)
            {
                Debug.LogError("invalid commandline arguments!!");
                return;
            }

            Debug.Log(opts.Value.InputFile);

            //Parser.Default.ParseArguments<Options>(args)
            //    .WithParsed(RunOptions)
            //    .WithNotParsed(HandleParseError);

        }

        static void RunOptions(Options opts)
        {
            //handle options
            Debug.Log(opts.InputFile);
        }
        static void HandleParseError(IEnumerable<Error> errs)
        {
            //handle errors
        }

        [MenuItem("Example/Build")]
        private static void Build()
        {
            var tasks = new List<IBuildTask>
            {
                // プラットフォームを切り替える
                new SwitchPlatform(),
                // 今のプラットフォームをログ出力する
                new LogPlatform()
            };

            var contexts = new BuildContext();
            // SwitchPlatform用のContextを追加する
            var switchPlatformContext = new SwitchPlatformContext(BuildTargetGroup.Android, BuildTarget.Android);
            contexts.SetContextObject(switchPlatformContext);

            var returnCode = BuildTasksRunner.Run(tasks, contexts);

            Debug.Log(returnCode);
        }
    }

    public class LogPlatform : IBuildTask
    {
        public int Version => 1;

        public ReturnCode Run()
        {
            Debug.Log(EditorUserBuildSettings.activeBuildTarget);
            return ReturnCode.Success;
        }
    }

    // プラットフォームを切り替えるBuildTask
    public class SwitchPlatform : IBuildTask
    {
        // DI対象のフィールドにはInjectContextを付ける
        // DIが任意の場合は第二引数をtrueに
        [InjectContext(ContextUsage.In)]
        private readonly ISwitchPlatformContext _context = null;

        public int Version => 1;

        public ReturnCode Run()
        {
            return EditorUserBuildSettings.SwitchActiveBuildTarget(_context.Group, _context.Target)
                ? ReturnCode.Success
                : ReturnCode.Error;
        }
    }

    // SwitchPlatform用のContextのインタフェース
    public interface ISwitchPlatformContext : IContextObject
    {
        BuildTargetGroup Group { get; }
        BuildTarget Target { get; }
    }

    // SwitchPlatform用のContextの実装クラス
    public class SwitchPlatformContext : ISwitchPlatformContext
    {
        public BuildTargetGroup Group { get; }
        public BuildTarget Target { get; }

        public SwitchPlatformContext(BuildTargetGroup group, BuildTarget target)
        {
            Group = group;
            Target = target;
        }
    }

    public interface IBuildUnityPlayerContext : IContextObject
    {
        BuildPlayerOptions Options { get; }
    }

    internal class BuildUnityPlayerContext : IBuildUnityPlayerContext
    {
        public BuildPlayerOptions Options { get; }
        public BuildUnityPlayerContext(BuildPlayerOptions options)
        {
            Options = options;
        }
    }

    public class BuildUnityPlayer : IBuildTask
    {
        public int Version => 1;

        [InjectContext(ContextUsage.In)]
        private readonly IBuildUnityPlayerContext ctx = null;

        public ReturnCode Run()
        {
            var report = BuildPipeline.BuildPlayer(ctx.Options);
            var summary = report.summary;
            var sb = new StringBuilder();
            sb.AppendLine($"BuildReport:");
            sb.AppendLine($"  Summary:");
            sb.AppendLine($"    Result: {summary.result}");
            sb.AppendLine($"    Platform: {summary.platform}");
            sb.AppendLine($"    TotalTime: {summary.totalTime}");
            sb.AppendLine($"    TotalSize: {summary.totalSize}");
            sb.AppendLine($"    OutputPath: {summary.outputPath}");
            sb.AppendLine($"  Steps:");
            foreach (var step in report.steps)
            {
                // depth2以上のものは細かすぎる(アセット毎に出たりする)ので省略
                if (step.depth >= 2) continue;

                sb.AppendLine($"    {step.name}:");
                sb.AppendLine($"      Depth: {step.depth}");
                sb.AppendLine($"      Duration: {step.duration}");
            }

            Debug.Log(sb);
            Debug.Log("<=====================");
            if (summary.result == BuildResult.Failed)
            {
                // batchModeで実行したときにexitコードを0以外にするために例外を投げる
                throw new Exception("BuildFailed");
            }

            return ReturnCode.Success;
        }
    }

    internal class Cli<T>
    //where T :
    {

    }

    class Options
    {
        [Option('i', "input", Required = true, HelpText = "入力するファイル名")]
        public string InputFile
        {
            get;
            set;
        }

        [Option('o', "output", Required = true, HelpText = "出力するファイル名")]
        public string OutputFile
        {
            get;
            set;
        }


        [Option("buildTarget", Required = true, HelpText = "build target")]
        public string BuildTarget
        {
            get;
            set;
        }


        [Option("projectPath", Required = true, HelpText = "project path")]
        public string ProjectPath
        {
            get;
            set;
        }

        [Option("executeMethod", Required = true, HelpText = "project path")]
        public string ExecuteMethod
        {
            get;
            set;
        }

        [Option("nographics", Default = false, HelpText = "project path")]
        public bool IsNoGraphics
        {
            get;
            set;
        }

        [Option("batchmode", Default = false, HelpText = "project path")]
        public bool IsBatchMode
        {
            get;
            set;
        }

        [Option("logfile", Default = false, HelpText = "project path")]
        public bool IsLogfile
        {
            get;
            set;
        }

        [Option("quit", Default = false, HelpText = "project path")]
        public bool IsQuit
        {
            get;
            set;
        }
    }

}

