export function addMeta(name: string, content: string) {
    let meta = document.querySelector(`meta[name = "${name}"`) as HTMLMetaElement;

    if (!meta)
        meta = (document.createElement('meta') as HTMLMetaElement);

    meta.name = name;
    meta.content = content;
    document.head.appendChild(meta);
}

export function sleep(duration: number) {
    return new Promise(resolve => {
        setTimeout(resolve, duration)
    })
}