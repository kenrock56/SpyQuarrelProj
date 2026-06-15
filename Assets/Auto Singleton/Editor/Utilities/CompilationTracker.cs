using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;

namespace AutoSingletonEditor
{
    [InitializeOnLoad]
    static class CompilationTracker
    {
        enum State
        {
            NeverCompiled,
            Compiling,
            Success,
            Failure,
        }

        static State state = State.NeverCompiled;

        public static bool HasAnyCompilationError => (state != State.Success);

        static CompilationTracker()
        {
            StaticDataSerializer.MaintainDataBetweenAssemblyReload(typeof(CompilationTracker), nameof(CompilationTracker));

            CompilationPipeline.compilationStarted += OnAnyAssemblyCompilationStarted;
        }

        static void OnAnyAssemblyCompilationStarted(object obj)
        {
            state = State.Compiling;

            CompilationPipeline.assemblyCompilationFinished += OnAssemblyCompilationFinished;
            CompilationPipeline.compilationFinished += OnAllAssembliesCompilationFinished;
        }

        static void OnAssemblyCompilationFinished(string asmPath, CompilerMessage[] compilerMessages)
        {
            if (compilerMessages.Any(cm => cm.type == CompilerMessageType.Error))
                state = State.Failure;
        }

        static void OnAllAssembliesCompilationFinished(object obj)
        {
            if (state == State.Compiling)
                state = State.Success;

            CompilationPipeline.assemblyCompilationFinished -= OnAssemblyCompilationFinished;
            CompilationPipeline.compilationFinished -= OnAllAssembliesCompilationFinished;
        }
    }
}
