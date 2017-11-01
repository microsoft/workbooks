# Test Case: atx-h1
# H1

# Test Case: atx-h2
## H2

# Test Case: atx-h3
### H3

# Test Case: atx-h4
#### H4

# Test Case: atx-h5
##### H5

# Test Case: atx-h6
###### H6

# Test Case: settext-h1-short
H1 Short
=

# Test Case: settext-h1-long
H1 Long
======

# Test Case: settext-h2-short
H2 Short
-

# Test Case: settext-h2-long
H2 Long
---------------

# Test Case: list-bullet-loose
* one

* two

* three

# Test Case: list-bullet-tight
* A
* B
* C

# Test Case: list-ordered-tight-period-left-align
7.  seven
8.  eight
9.  nine
10. ten

# Test Case: list-ordered-tight-period-right-align
 7. seven
 8. eight
 9. nine
10. ten

# Test Case: list-ordered-loose-parenthesis
1) one

2) two

3) three

# Test Case: list-multi-line-items
* this is a list item with a soft break where the second
  line is indented two characters to align with the first
  here is a third line
  and a fourth
* this is a single line
* and here is another with a hard break\
  but the second line still aligns

# Test Case: list-with-all-item-padding
   * items are prefixed with three pad spaces
     including the bullet line
  * this item however only has two pads
    with no second item padding
 * this item has one padding
* and this one has none

# Test Case: list-with-sub-lists
  * A
    - A.A
      + A.A.A
      + A.A.B
    - A.B
    - A.C
  * B
    - B.A
    - B.B
      + B.B.A
        * B.B.A.A
      + B.B.B
    - B.C
  * C

# Test Case: blockquote-simple
> simple

# Test Case: blockquote-deep
> # H1 in Blockquote
>
> Paragraph in blockquote with soft break
> to continue on the next line but render
> on the same line.
>
> Another Paragraph
>
> > Nested quote
> >
> > > Three deep

# Test Case: block-quote-with-code-fence
> Paragraph
>
> >    ```csharp
> >    2 + 2
> >    ```
>
> Paragraph

# Test Case: code-fence-ticks
```
vanilla fenced block
```

# Test Case: code-fence-tilde
~~~
vanilla fenced block
  alternate style
	  prefixing with whitespace
~~~

# Test Case: code-fence-leading-space
  ```javascript extra-info
  function () {
    return "woo"
  }
  ```

# Test Case: code-fence-with-fence-in-content

````markdown
```csharp
2 + 2
```
````

# Test Case: code-indented-with-spaces-with-leading-space-content
        This is an indented code block
        and there is nothing special about it
        exceptits contents are also indented

# Test Case: html
<div style='color: green'>
  <b>This is &amp;n HTML block</b>
</div>

# Test Case: reference-defintion *[skip]*

> **NOTE**: this case is ignored since we cannot actually preserve reference
  definitions at all currently.
>
> Due to bugs in CommonMark.NET the tests below must actually conform
to the lossy transformations that CommonMark.NET will make. For instance,
the keys must be upper cased and the title must use double quotes. In fact,
the test might be broken altogether as well since the references are stored
in a Dictionary<K,V>, which has no insertion order guarantees. Ugh.

[A]: b
[*EM*]: http://catoverflow.com
[FOO*BAR\]]: my_(url) "title (with parens)"

# Test Case: inline-soft-break
This is a paragraph with a
soft break.

# Test Case: inline-hard-break
This paragraph has\
Hard breaks producing three lines\
Here is your haiku

# Test Case: inline-emphasis
*bold _underscore_ bold*

# Test Case: inline-code-simple
`hello`

# Test Case: inline-code-escape-single-tick
`` ` ``

# Test Case: inline-code-escape-many-ticks
```` ``hello`` ` ```ticks``` ````

# Test Case: inline-html
Hello <span style='color:hotpink'>hot pink</span> <strike>text</strike>.

# Test Case: inline-strike
Hello ~~strike~~

# Test Case: inline-placeholder
This is a [custom] directive.

# Test Case: inline-link
Here is a [link to *somewhere*](http://catoverflow.com "Best Cat Site").

# Test Case: inline-image
![image title](/foo.jpg "image alt")