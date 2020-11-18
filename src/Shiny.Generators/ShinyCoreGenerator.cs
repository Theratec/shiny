﻿using System;
using System.Collections.Generic;
using Shiny.Generators.Tasks;
//using Shiny.Generators.Tasks.iOS;
//using Shiny.Generators.Tasks.Android;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;


namespace Shiny.Generators
{
    [Generator]
    public class ShinyCoreGenerator : ISourceGenerator
    {
        readonly IList<ShinySourceGeneratorTask> tasks = new List<ShinySourceGeneratorTask>
        {
            //new AutoStartupTask(),
            ////new PrismBridgeTask(),

            //new AppDelegateTask(),
            //new ApplicationTask(),
            //new ActivityTask()
        };


        public void Execute(GeneratorExecutionContext context)
        {
            if (context.Compilation.Language != LanguageNames.CSharp)
                return;

            // retreive the populated receiver
            if (!(context.SyntaxReceiver is ShinySyntaxReceiver receiver))
                return;

            //var workspace = Workspace.GetWorkspaceRegistration(context.Compilation.);
            //workspace.Workspace.Kind == WorkspaceKind.MSBuild
            //workspace.Workspace.CurrentSolution.Projects.
            var shinyContext = new ShinyContext(context);

            // always first
            //new AutoStartupTask().Init(shinyContext);

            var tasks = new List<Task>();
            foreach (var task in this.tasks)
            {
                tasks.Add(Task.Run(() =>
                {
                    try
                    {
                        task.Init(shinyContext);
                        task.Execute();
                    }
                    catch (Exception ex)
                    {
                        //shinyContext.Log.Warn($"{task.GetType().FullName} Exception - {ex}");
                    }
                }));
            }
            Task.WhenAll(tasks.ToArray()).GetAwaiter().GetResult();
        }


        public void Initialize(GeneratorInitializationContext context)
        {
            // Register a syntax receiver that will be created for each generation pass
            context.RegisterForSyntaxNotifications(() => new ShinySyntaxReceiver());
        }
    }
}
