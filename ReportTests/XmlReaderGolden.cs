namespace ReportTests
{
    /// <summary>Output of the original XPathDocument based XmlDataReader, see XmlReaderShapeTests.</summary>
    static class XmlReaderGolden
    {
        public const string Text = """
### elements
reader 1
  cols=Id:String,Name:String
  '1' | 'a'
  '2' | 'b'
  '3' | 'c'
reader 2
  cols=Id:String,Name:String
  '1' | 'a'
  '2' | 'b'
  '3' | 'c'

### elements-type
reader 1
  cols=Id:String,Name:String
  '1' | 'a'
  '2' | 'b'
  '3' | 'c'
reader 2
  cols=Id:String,Name:String
  '1' | 'a'
  '2' | 'b'
  '3' | 'c'

### relative-path
reader 1
  cols=Id:String,Name:String
  '1' | 'a'
  '2' | 'b'
  '3' | 'c'
reader 2
  cols=Id:String,Name:String
  '1' | 'a'
  '2' | 'b'
  '3' | 'c'

### attributes
reader 1
  cols=id:String,name:String
  '1' | 'a'
  '2' | 'b'
reader 2
  cols=id:String,name:String
  '1' | 'a'
  '2' | 'b'

### both
reader 1
  cols=id:String,name:String
  '1' | 'a'
  '2' | 'b'
reader 2
  cols=id:String,name:String
  '1' | 'a'
  '2' | 'b'

### both-name-clash
reader 1
  cols=id:String,id_1:String
  '1' | 'x'
  '2' | 'y'
reader 2
  cols=id:String,id_1:String
  '1' | 'x'
  '2' | 'y'

### pretty-printed
reader 1
  cols=Id:String,Name:String
  ' 1 ' | 'a b'
  '2' | ''
reader 2
  cols=Id:String,Name:String
  ' 1 ' | 'a b'
  '2' | ''

### xml-space-preserve
reader 1
  cols=a:String,b:String
  '  ' | ''
reader 2
  cols=a:String,b:String
  '  ' | ''

### nested-value
reader 1
  cols=a:String,c:String
  'xyz' | 'deep'
reader 2
  cols=a:String,c:String
  'xyz' | 'deep'

### cdata-entities
reader 1
  cols=a:String,b:String,c:String
  '<raw & text>' | '& < A B' | 'pqr'
reader 2
  cols=a:String,b:String,c:String
  '<raw & text>' | '& < A B' | 'pqr'

### empty-elements
reader 1
  cols=a:String,b:String,c:String
  '' | '' | ''
reader 2
  cols=a:String,b:String,c:String
  '' | '' | ''

### missing-and-extra
reader 1
  cols=a:String,b:String
  '1' | '2'
  NULL | '3'
  '4' | '5'
reader 2
  cols=a:String,b:String
  '1' | '2'
  NULL | '3'
  '4' | '5'

### duplicate-children
reader 1
  cols=a:String,a_1:String,b:String
  '1' | '1' | '3'
  '4' | '4' | '6'
reader 2
  cols=a:String,a_1:String,b:String
  '1' | '1' | '3'
  '4' | '4' | '6'

### default-namespace-root
reader 1
  cols=rows:String
reader 2
  cols=rows:String

### default-namespace-child
reader 1
  cols=a:String
  '2'
reader 2
  cols=a:String
  '2'

### prefixed-children
reader 1
  cols=p:a:String,b:String
  '1' | '2'
  '3' | '4'
reader 2
  EXCEPTION XPathException

### prefixed-attributes
reader 1
  cols=p:a:String,b:String
  EXCEPTION XPathException
reader 2
  EXCEPTION XPathException

### xmlns-attributes
reader 1
  cols=
reader 2
  cols=

### columns-simple
reader 1
  cols=Name:String,Id:String
  'a' | '1'
  'b' | '2'
  'c' | '3'
reader 2
  cols=Name:String,Id:String
  'a' | '1'
  'b' | '2'
  'c' | '3'

### columns-missing-first-row
reader 1
  cols=b:String,a:String
  NULL | '1'
  '3' | '2'
reader 2
  cols=b:String,a:String
  NULL | '1'
  '3' | '2'

### columns-attribute
reader 1
  cols=id:String,n:String,@zz:String
  '1' | 'a' | NULL
  '2' | 'b' | NULL
reader 2
  cols=id:String,n:String,@zz:String
  '1' | 'a' | NULL
  '2' | 'b' | NULL

### columns-duplicates
reader 1
  cols=Id:String,Id_1:String,Name:String
  '1' | '1' | 'a'
  '2' | '2' | 'b'
  '3' | '3' | 'c'
reader 2
  cols=Id:String,Id_1:String,Name:String
  '1' | '1' | 'a'
  '2' | '2' | 'b'
  '3' | '3' | 'c'

### columns-parent
reader 1
  cols=a:String,t:String
  '1' | 'T'
reader 2
  cols=a:String,t:String
  '1' | 'T'

### columns-function
reader 1
  EXCEPTION XPathException
reader 2
  EXCEPTION XPathException

### rows-descendant
reader 1
  cols=x:String
  '1'
  '2'
reader 2
  cols=x:String
  '1'
  '2'

### rows-predicate
reader 1
  cols=Id:String,Name:String
  '2' | 'b'
reader 2
  cols=Id:String,Name:String
  '2' | 'b'

### rows-wildcard
reader 1
  cols=Id:String,Name:String
  '1' | 'a'
  '2' | 'b'
  '3' | 'c'
reader 2
  cols=Id:String,Name:String
  '1' | 'a'
  '2' | 'b'
  '3' | 'c'

### deep-path
reader 1
  cols=x:String
  '1'
reader 2
  cols=x:String
  '1'

### rows-at-other-depths
reader 1
  cols=x:String,row:String
  '1' | 'nested'
reader 2
  cols=x:String,row:String
  '1' | 'nested'

### no-rows
reader 1
  cols=rows:String
reader 2
  cols=rows:String

### wrong-root
reader 1
  cols=other:String
reader 2
  cols=other:String

### doctype-entity
reader 1
  cols=a:String
  'expanded'
reader 2
  cols=a:String
  'expanded'

### text-in-row
reader 1
  cols=a:String
  '1'
reader 2
  cols=a:String
  '1'

### unicode
reader 1
  cols=a:String
  'héllo 😀'
reader 2
  cols=a:String
  'héllo 😀'

### hyphen-dot-names
reader 1
  cols=first-name:String,my.val:String
  'a' | 'b'
reader 2
  cols=first-name:String,my.val:String
  'a' | 'b'

### malformed
reader 1
  EXCEPTION XmlException
reader 2
  EXCEPTION XmlException

""";
    }
}
