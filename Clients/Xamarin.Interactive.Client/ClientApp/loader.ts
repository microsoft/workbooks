//
// loader.ts
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

import { xiexports } from "./xiexports"

class ResourceLoader {
  loadAsync(source: string, callback?: () => void) {
    const dispatchCallback = () => {
      console.log(`ResourceLoader: ${performance.now()}: finish load: ${source}`)
      if (callback)
        callback()
    }

    console.log(`ResourceLoader: ${performance.now()}: start load: ${source}`)
    switch (source.substring(source.lastIndexOf('.') + 1)) {
      case "js": {
        const elem = document.createElement("script")
        elem.async = true
        elem.type = "text/javascript"
        elem.src = source
        elem.onload = dispatchCallback
        document.head.appendChild(elem)
        break
      }

      case "css": {
        const elem = document.createElement("link")
        elem.rel = "stylesheet"
        elem.href = source
        document.head.appendChild(elem)

        // Unlike JS, we can actually continue immediately. When
        // the browser actually loads the CSS, it will query all
        // selectors and restyle as appropriate.
        dispatchCallback()
        break
      }
    }
  }
}

xiexports.ResourceLoader = new ResourceLoader