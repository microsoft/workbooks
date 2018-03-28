declare module "draft-js-plugins-editor";
declare module "draft-js-markdown-plugin";
declare module "turndown";

/* 
    TextDecoder bits borrowed from https://github.com/Microsoft/TSJS-lib-generator/pull/300/files,
    as TypeScript does not appear to have them yet, but they are standard Web/DOM APIs.
*/
interface TextDecodeOptions {
    stream?: boolean;
}

interface TextDecoderOptions {
    fatal?: boolean;
    ignoreBOM?: boolean;
}

interface TextDecoder {
    readonly encoding: string;
    readonly fatal: boolean;
    readonly ignoreBOM: boolean;
    decode(input?: BufferSource, options?: TextDecodeOptions): USVString;
}

declare var TextDecoder: {
    prototype: TextDecoder;
    new(label?: string, options?: TextDecoderOptions): TextDecoder;
};