import { WebAssemblyAgent } from "./agent"
import { assemblies } from "./assemblies";
import { Handle, MonoRuntime, MonoString } from "./runtime";

declare global {
    interface Window {
        Module: any
        EvaluationObserver: any
        queuedMessages: any[]
    }
}

var Module: any = {
    assemblies: assemblies,
    queuedMessages: [],
    runtime: null,
    agent: null,
    messagePort: null,

    // Borrowed this from emscripten.
    basename(path: string): string {
        if (path === '/')
            return '/'
        var lastSlash = path.lastIndexOf('/');
        if (lastSlash === -1)
            return path
        return path.substr(lastSlash + 1)
    },

    createDataFile(root: string, name: string, data: Uint8Array, canRead: boolean, canWrite: boolean, canOwn: boolean) {
        try {
            Module.FS_createDataFile(root, name, data, canRead, canWrite, canOwn)
        } catch (e) {
            // Check to see if it's an EEXISTS error.
            const { errno } = e

            if (!errno || errno !== 17 /* EEXISTS */)
                throw e
        }
    },

    async loadAssembly(assemblyName: string, createLocation: (string | undefined)) : Promise<void> {
        const response = await fetch(assemblyName, { credentials: 'same-origin' })
        if (!response.ok)
            throw new Error(`Failed to load assembly ${assemblyName}!`)
        const asm = new Uint8Array(await response.arrayBuffer())

        Module.createDataFile("managed", createLocation || Module.basename(assemblyName), asm, true, true, true)
    },

    async onRuntimeInitialized() {
        Module.messagePort.postMessage({ $type: "LoadingMessage" })

        Module.FS_createPath("/", "managed", true, true)

        for (const assemblyName of <string[]>Module.assemblies)
            await Module.loadAssembly(assemblyName)

        Module.bclLoadingDone()
    },

    bclLoadingDone() {
        try {
            const runtime: MonoRuntime = new MonoRuntime()
            Module.runtime = runtime
            Module.agent = Module.initWasmAgent(runtime)
        } catch (e) {
            Module.messagePort.postMessage({
                $type: "ErrorMessage",
                error: e
            })
        }

        if (Module.queuedMessages.length > 0)
            Module.queuedMessages.forEach((qm: any) => Module.agent.processMessage(qm))

        Module.messagePort.postMessage({ $type: "ReadyMessage" })
    },

    initWasmAgent(runtime: MonoRuntime): WebAssemblyAgent {
        const mainModule = runtime.loadAssembly("Xamarin.Workbooks.WebAssembly")
        if (!mainModule.valid)
            throw "Could not find main module " + this.mainAssembly;

        const wasmClass = mainModule.findClass("Xamarin.Workbooks.WebAssembly", "WebAssemblyAgent")
        if (!wasmClass.valid)
            throw "Could not find WASM entry class"

        const genericAgent = runtime.createObject(wasmClass)

        const wasmAgentConstructor = wasmClass.findMethod(".ctor")
        if (!wasmAgentConstructor.valid)
            throw "Could not find WASM agent constructor"

        const wasmHandle = wasmAgentConstructor.invoke(genericAgent)
        return new WebAssemblyAgent(genericAgent.handle, runtime)
    },

    logMessage(logHandle: number) {
        const logJson = new MonoString(new Handle(logHandle), Module.runtime).toString()
        const log = JSON.parse(logJson);

        switch (log.entry.level) {
            case "Error":
                console.error(log.toString)
                break
            case "Debug":
            case "Verbose":
                console.debug(log.toString)
                break
            case "Warning":
                console.warn(log.toString)
                break
            default:
                console.log(log.toString)
                break
        }
    }
}

window.addEventListener("message", (event) => {
    const data = event.data || {}
    const { $type } = data

    if ($type === "ChannelOpenMessage") {
        Module.messagePort = <MessagePort>(event.ports[0])
        Module.messagePort.onmessage = (ev: MessageEvent) => {
            if (Module.agent)
                Module.agent.processMessage(ev.data)
            else
                Module.queuedMessages.push(ev.data)
        }
    }
});

window.Module = Module
window.EvaluationObserver = {
    onException: function(exception: string) {
        console.log(exception)
    },
    onNext: function(event: any) {
        if (typeof event === 'number') {
            const objectJson = new MonoString(new Handle(<number>event), Module.runtime).toString()
            const object = JSON.parse(objectJson)
            Module.messagePort.postMessage(object)
        }
    }
}