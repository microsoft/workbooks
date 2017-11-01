//
// node-app.ts
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.

declare var global: any

import { WorkbookDocument } from "./workbook/dom"

if (global && global.process && global.process.title === "node") {
  // stub enough browser environment to run under Node
  global.window = {}
  global.navigator = {}
  global.document = {
    createElement: (tagName: string) => ({ className: "" })
  }

  runTests()
}

function runTests() {
  const doc = WorkbookDocument.read(`---
yaml line 0
yaml line 1
---

# H1 with <del>inline HTML</del>!

\`\`\`csharp xw-cell
a code submission
line two
\`\`\`

## H2 more markdown

Here's a paragraph

* one
* two
* three

\`\`\`csharp
more code
\`\`\`

---

---
some yaml
---

Ending paragraph`)
  console.log(doc)
}