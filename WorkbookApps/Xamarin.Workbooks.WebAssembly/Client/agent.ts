import { ManagedObject, Handle, MonoRuntime, MarshalType } from "./runtime";

declare const Module: any;

export class WebAssemblyAgent extends ManagedObject {
    constructor(handle: Handle, runtime: MonoRuntime) {
        const asm = runtime.loadAssembly("Xamarin.Workbooks.WebAssembly")
        const klass = asm.findClass("Xamarin.Workbooks.WebAssembly", "WebAssemblyAgent")
        super(klass, handle, runtime)
    }

    async processMessage(message: any): Promise<void> {
        const kind = <string>message.kind
        const data = <any>message.data
        const $type = data ? <string>data.$type : null

        if (!data)
            return

        switch (kind) {
            case "InitializingWorkspace":
                if ($type && $type !== "Xamarin.Interactive.CodeAnalysis.TargetCompilationConfiguration")
                    return

                delete data.globalStateType.resolvedType
                const tcc = JSON.stringify(data)
                this.initialize(tcc)
                break
            case "Evaluation":
                if ($type && $type !== "Xamarin.Interactive.CodeAnalysis.Compilation")
                    return

                const compilation = data;
                const references = <any[]>compilation.references;

                for (const assembly of references) {
                    const assemblyContent = assembly.content
                    const location = assemblyContent.location

                    const assemblyFetchUrl = `/api/assembly/get?path=${encodeURIComponent(location)}`
                    await Module.loadAssembly(assemblyFetchUrl, Module.basename(location))

                    assemblyContent.location = `/managed/${Module.basename(location)}`
                }

                const compilationJson = JSON.stringify(compilation)
                this.evaluate(compilationJson)
                break
        }
    }

    initialize(targetCompilationConfiguration: string): void {
        const initializeMethod = this.class.findMethod("Initialize")
        initializeMethod.invoke(this, [ MarshalType.String ], targetCompilationConfiguration)
    }

    evaluate(compilation: string): void {
        const evaluateMethod = this.class.findMethod("Evaluate")
        evaluateMethod.invoke(this, [ MarshalType.String ], compilation)
    }
}