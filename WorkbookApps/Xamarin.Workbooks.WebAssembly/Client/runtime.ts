// TODO: Write typings for Module bits, figure out
// how to pass it through webpack untouched.
declare const Module: any;

export const enum MarshalType {
    String,
    Int,
    Long,
    Float,
    Double,
    Handle
}

export class Handle {
    private readonly _value: number

    get value(): number {
        return this._value
    }

    constructor(value: number) {
        this._value = value
    }
}

export const enum MonoObjectType {
    Integer = 1,
    FloatingPoint = 2,
    String = 3,
    ReferenceType = 4,
    ValueType = 5
}

class MonoApiObject {
    private _handle: Handle
    protected runtime: MonoRuntime

    get handle(): Handle {
        return this._handle
    }

    get valid(): boolean {
        return this.runtime && this._handle.value !== 0
    }

    get runtimeType(): MonoObjectType {
        return this.runtime.getObjectType(this.handle.value)
    }

    constructor(handle: Handle, runtime: MonoRuntime) {
        this._handle = handle
        this.runtime = runtime
    }
}

export class MonoAssembly extends MonoApiObject {
    private _name: string

    public get name(): string {
        return this._name
    }

    constructor(assemblyName: string, handle: Handle, runtime: MonoRuntime) {
        super(handle, runtime)
        this._name = assemblyName
    }

    findClass(namespace: string, name: string): MonoClass {
        return this.runtime.findClass(this, namespace, name)
    }
}

export class MonoClass extends MonoApiObject {
    private _assembly: MonoAssembly
    private _namespace: string
    private _name: string

    public get assembly(): MonoAssembly {
        return this._assembly
    }

    public get namespace(): string {
        return this._namespace
    }

    public get name(): string {
        return this._name
    }

    constructor(assembly: MonoAssembly, namespace: string, name: string, handle: Handle, runtime: MonoRuntime) {
        super(handle, runtime)
        this._namespace = namespace
        this._name = name
        this._assembly = assembly
    }

    findMethod(name: string, argc: number = -1): MonoMethod {
        return this.runtime.findMethod(this, name, argc)
    }
}

export class MonoMethod extends MonoApiObject {
    private _assembly: MonoAssembly
    private _class: MonoClass
    private _name: string
    private _argc: number

    public get assembly(): MonoAssembly {
        return this._assembly
    }

    public get class(): MonoClass {
        return this._class
    }

    public get name(): string {
        return this._name
    }

    public get argc(): number {
        return this._argc
    }

    constructor(assembly: MonoAssembly, klass: MonoClass, name: string, argc: number, handle: Handle, runtime: MonoRuntime) {
        super(handle, runtime)
        this._assembly = assembly
        this._class = klass
        this._name = name
        this._argc = argc
    }

    private callMethod(method: MonoMethod, thisArg: (Handle | null), argsMarshal: MarshalType[], ...args: (Handle | number | string)[]) {
        let extraArgsMemSize = 0
        for (var i = 0; i < args.length; ++i) {
            // long/double memory must be 8 bytes aligned and I'm being lazy here. - kumpera
            // allocate extra memory size for all but string and handle, to keep the conditional more readable - bojan
            if (argsMarshal[i] !== MarshalType.String && argsMarshal[i] !== MarshalType.Handle)
                extraArgsMemSize += 8
        }

        const extraArgsMem = extraArgsMemSize ? Module._malloc(extraArgsMemSize) : 0
        let extraArgIndex = 0;

        const argsMemory = Module._malloc(args.length * 4)
        var ehThrow = Module._malloc(4)
        for (var i = 0; i < args.length; ++i) {
            if (argsMarshal[i] === MarshalType.String) {
                if (typeof args[i] !== "string")
                    throw new Error(`Type of argument ${i} is ${typeof args[i]}, but string marshalling was requested!`)
                Module.setValue(argsMemory + i * 4, this.runtime.convertStringToMonoString(<string>args[i]), "i32")
            } else if (argsMarshal[i] === MarshalType.Handle) {
                if (typeof args[i] === "string" || typeof args[i] === "number")
                    throw new Error(`Type of argument ${i} is ${typeof args[i]}, but handle marshalling was requested!`)
                Module.setValue(argsMemory + i * 4, (<Handle>args[i]).value, "i32")
            } else {
                if (typeof args[i] !== "number")
                    throw new Error(`Type of argument ${i} is ${typeof args[i]}, but number marshalling was requested!`)

                // upstream has an else if here with a bigger conditional, but having limited the
                // enum here, we don't need to do that. we'll need to treat the extra cell
                // specially for int/long/float/double values, but for handles (the remaining enum member)
                // we can ignore the extra cell bit and just set the arg memory normally - bojan
                const extraCell = extraArgsMemSize + extraArgIndex
                extraArgIndex += 8

                if (argsMarshal[i] === MarshalType.Int)
                    Module.setValue(extraCell, <number>args[i], "i32")
                else if (argsMarshal[i] === MarshalType.Long)
                    Module.setValue(extraCell, <number>args[i], "i64")
                else if (argsMarshal[i] === MarshalType.Float)
                    Module.setValue(extraCell, <number>args[i], "float")
                else if (argsMarshal[i] === MarshalType.Double)
                    Module.setValue(extraCell, <number>args[i], "double")

                Module.setValue(argsMemory + i * 4, extraCell, "i32")
            }
        }
        Module.setValue(ehThrow, 0, "i32")

        const res = this.runtime.invokeMethod(method.handle.value, (thisArg ? thisArg.value : null), argsMemory, ehThrow)
        const ehRes = Module.getValue(ehThrow, "i32")

        if (extraArgsMemSize)
            Module._free(extraArgsMem)
        Module._free(argsMemory)
        Module._free(ehThrow)

        if (ehRes != 0) {
            const msg = new MonoString(new Handle(res), this.runtime).toString()
            throw new Error(msg)
        }

        if (!res)
            return res

        const objectType = this.runtime.getObjectType(res)
        switch (objectType) {
            case MonoObjectType.Integer:
                return this.runtime.unboxInteger(res)
            case MonoObjectType.FloatingPoint:
                return this.runtime.unboxFloat(res)
            case MonoObjectType.String:
                return new MonoString(new Handle(res), this.runtime).toString()
            default:
                return new Handle(res);
        }
    }

    invoke(thisArg: ManagedObject | null, argsMarshal: MarshalType[] = [], ...args: (Handle | string | number)[]): (Handle | number | string) {
        return this.callMethod(this, (thisArg ? thisArg.handle : null), argsMarshal, ...args)
    }
}

export class MonoString extends MonoApiObject {
    private cachedString: (string | null)

    constructor(strOrHandle: (string | Handle), runtime: MonoRuntime) {
        if (typeof strOrHandle === 'string') {
            const handle = new Handle(runtime.convertStringToMonoString(<string>strOrHandle))
            super(handle, runtime)
        } else
            super(<Handle>strOrHandle, runtime)

        this.cachedString = null
    }

    toString(): string {
        if (!this.valid)
            throw new Error("Tried to convert null string")

        if (this.cachedString)
            return this.cachedString

        const rawString = this.runtime.convertMonoStringToUtf8(this.handle.value)
        this.cachedString = Module.UTF8ToString(rawString)

        // We're responsible for freeing this, see driver.c
        Module._free(rawString)

        if (this.cachedString)
            return this.cachedString

        throw new Error("Could not convert string, should not be reached")
    }
}

export class ManagedObject {
    private _handle: Handle
    protected runtime: MonoRuntime
    protected assembly: MonoAssembly
    protected class: MonoClass
    private corlib: MonoAssembly
    private object: MonoClass
    private _toString: MonoMethod

    get handle(): Handle {
        return this._handle
    }

    get valid(): boolean {
        return this.runtime && this._handle.value !== 0
    }

    get type(): Type {
        const type = this.corlib.findClass("System", "Type")

        let getType: MonoMethod
        try {
            getType = this.class.findMethod("GetType")
        } catch {
            getType = this.object.findMethod("GetType")
        }

        // This is always a handle, because Type is a ref type. See the
        // switch at the end of callMethod.
        return new Type(type, <Handle>getType.invoke(this), this.runtime)
    }

    get typeFullName(): string {
        return this.runtime.getTypeFullName(this.handle.value)
    }

    toString(): string {
        return <string>this._toString.invoke(this)
    }

    constructor(klass: (MonoClass | undefined), handle: Handle, runtime: MonoRuntime) {
        this.class = klass ? klass : runtime.getClassForObject(handle.value)
        this.assembly = this.class.assembly
        this._handle = handle
        this.runtime = runtime

        this.corlib = this.runtime.loadAssembly("mscorlib")
        this.object = this.corlib.findClass("System", "Object")

        try {
            this._toString = this.class.findMethod("ToString")
        } catch {
            this._toString = this.object.findMethod("ToString")
        }
    }
}

export class Type extends ManagedObject {
    toString(): string {
        const toStringMethod = this.class.findMethod("ToString")
        return <string>toStringMethod.invoke(this)
    }

    get fullName(): string {
        const fullNameGetter = this.class.findMethod("get_FullName")
        return <string>fullNameGetter.invoke(this)
    }
}

export class MonoRuntime {
    private assemblyCache: any = {}
    private classCache: any = {}
    private methodCache: any = {}

    constructor() {
        this._loadRuntime = Module.cwrap('mono_wasm_load_runtime', null, ['string', 'number'])
        this._loadAssembly = Module.cwrap('mono_wasm_assembly_load', 'number', ['string'])
        this._findClass = Module.cwrap('mono_wasm_assembly_find_class', 'number', ['number', 'string', 'string'])
        this._findMethod = Module.cwrap('mono_wasm_assembly_find_method', 'number', ['number', 'string', 'number'])
        this._getClass = Module.cwrap('mono_wasm_get_class', 'number', ['number'])
        this._getType = Module.cwrap('mono_wasm_get_type', 'number', ['number'])
        this._getTypeFullName = Module.cwrap('mono_wasm_get_type_full_name', 'number', ['number'])
        this._getAssemblyFromClass = Module.cwrap('mono_wasm_get_assembly_from_class', 'number', ['number'])
        this._getAssemblyName = Module.cwrap('mono_wasm_get_assembly_name', 'number', ['number'])
        this._getClassNamespace = Module.cwrap('mono_wasm_get_class_namespace', 'number', ['number'])
        this._getClassName = Module.cwrap('mono_wasm_get_class_name', 'number', ['number'])
        this._createObject = Module.cwrap('mono_wasm_object_new', 'number', ['number'])
        this.invokeMethod = Module.cwrap('mono_wasm_invoke_method', 'number', ['number', 'number', 'number', 'number'])
        this.convertMonoStringToUtf8 = Module.cwrap('mono_wasm_string_get_utf8', 'number', ['number'])
        this.convertStringToMonoString = Module.cwrap('mono_wasm_string_from_js', 'number', ['string'])
        this.getObjectType = Module.cwrap('mono_wasm_get_obj_type', 'number', ['number'])
        this.unboxInteger = Module.cwrap('mono_wasm_unbox_int', 'number', ['number'])
        this.unboxFloat = Module.cwrap('mono_wasm_unbox_float', 'number', ['number'])

        this._loadRuntime("managed", 1)
    }

    // These are all fake definitions—the constructor will replace them with proper method
    // calls via the WASM C wrapping interface. They're here because this is easier a bunch
    // of function-typed "fields"
    private _loadRuntime(managedPath: string, enableDebugging: number): void { }
    private _loadAssembly(assemblyName: string): number { return 0; }
    private _findClass(assembly: number, namespace: string, name: string): number { return 0 }
    private _findMethod(klass: number, name: string, argc: number): number { return 0 }
    private _getTypeFullName(type: number): number { return 0 }
    private _getClass(object: number): number { return 0 }
    private _getType(klass: number): number { return 0 }
    private _getAssemblyFromClass(klass: number): number { return 0 }
    private _getAssemblyName(assembly: number): number { return 0 }
    private _getClassNamespace(klass: number): number { return 0 }
    private _getClassName(klass: number): number { return 0 }
    _createObject(klass: number): number { return 0 }
    unboxInteger(int: number): number { return 0 }
    unboxFloat(float: number): number { return 0 }
    getObjectType(object: number): MonoObjectType { return 0 }
    invokeMethod(method: number, thisArg: (number | null), params: number, gotException: number): number { return 0 }
    convertMonoStringToUtf8(string: number): number { return 0 }
    convertStringToMonoString(str: string): number { return 0 }

    getTypeFullName(obj: number): string {
        const klass = this._getClass(obj)
        const type = this._getType(klass)
        const typeName = this._getTypeFullName(type)
        const jsString = Module.UTF8ToString(typeName)
        return jsString
    }

    getClassForObject(obj: number): MonoClass {
        const klass = this._getClass(obj)
        const assembly = this._getAssemblyFromClass(klass)
        const assemblyName = Module.UTF8ToString(this._getAssemblyName(assembly))
        const classNamespace = Module.UTF8ToString(this._getClassNamespace(klass))
        const className = Module.UTF8ToString(this._getClassName(klass))

        const monoAssembly = new MonoAssembly(
            assemblyName,
            new Handle(assembly),
            this)

        return new MonoClass(
            monoAssembly,
            classNamespace,
            className,
            new Handle(klass),
            this
        )
    }

    loadAssembly(assemblyName: string): MonoAssembly {
        let asmHandle = this.assemblyCache[assemblyName]
        if (!asmHandle) {
            asmHandle = new Handle(this._loadAssembly(assemblyName))

            if (!asmHandle.value)
                throw new Error(`Could not load assembly ${assemblyName}`)

            this.assemblyCache[assemblyName] = asmHandle;
        }

        return new MonoAssembly(assemblyName, asmHandle, this)
    }

    findClass(assembly: MonoAssembly, namespace: string, name: string): MonoClass {
        const key = `${assembly.handle}_${namespace}_${name}`

        let klass = this.classCache[key]
        if (!klass) {
            klass = new Handle(this._findClass(assembly.handle.value, namespace, name))

            if (!klass.value)
                throw new Error(`Could not load class ${namespace}.${name} from ${assembly.name}`)

            this.classCache[key] = klass
        }

        return new MonoClass(assembly, namespace, name, klass, this)
    }

    createObject(klass: MonoClass): ManagedObject {
        const newObject = new Handle(this._createObject(klass.handle.value))
        return new ManagedObject(klass, newObject, this)
    }

    findMethod(klass: MonoClass, name: string, argc: number): MonoMethod {
        const key = `${klass.handle}_${name}_${argc}`

        let method = this.methodCache[key]
        if (!method) {
            method = new Handle(this._findMethod(klass.handle.value, name, argc))

            if (!method.value)
                throw new Error(
                    `Could not load method ${name} with ${argc} argument(s) from class ` +
                    `${klass.namespace}.${klass.name} in ${klass.assembly.name}`)

            this.methodCache[key] = method
        }

        return new MonoMethod(klass.assembly, klass, name, argc, method, this)
    }

    /**
     * Boxes a number so it can be passed to Mono. Must be freed by the caller!
     * @param number The number to box
     * @returns A pointer to some memory with the integer boxed into it.
     */
    boxInteger(number: number): number {
        const memory: number = Module._malloc(4)
        Module.setValue(memory, number, "i32")
        return memory
    }

    /**
     * Boxes a boolean so it can be passed to Mono. Must be freed by the caller!
     * @param bool The boolean to box
     * @returns A pointer to some memory with the boolean boxed into it.
     */
    boxBoolean(bool: boolean): number {
        const memory: number = Module._malloc(1)
        Module.setValue(memory, bool, "i8")
        return memory
    }

    free(ptr: number): void {
        Module._free(ptr)
    }
}