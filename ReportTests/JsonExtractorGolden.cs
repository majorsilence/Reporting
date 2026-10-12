namespace ReportTests
{
    /// <summary>Output of the original dictionary based JsonTableExtractor, see JsonExtractorShapeTests.</summary>
    static class JsonExtractorGolden
    {
        public const string Text = """
### flat
table root cols=__guid:String,Id:Int64,Name:String,Price:Double,Ok:Boolean
  guid:g1 | Int64:1 | String:a | Double:1.5 | Boolean:True
  guid:g2 | Int64:2 | String:b | Int64:2 | Boolean:False

### varying-keys
table root cols=__guid:String,a:Int64,b:Int64
  guid:g1 | Int64:1 | Int64:2
  guid:g2 | NULL | Int64:3
  guid:g3 | Int64:5 | NULL

### first-row-nulls
table root cols=__guid:String,a:Int64,b:String
  guid:g1 | NULL | String:x
  guid:g2 | Int64:7 | NULL

### object-root
table root cols=__guid:String,Id:Int64,Name:String,Inner:Dictionary`2,Inner_k:Int64
  guid:g1 | Int64:1 | String:solo | {k=Int64:1,deep=String:{"z":2}} | Int64:1
table root_Inner_deep cols=__parent_guid:String,z:Int64
  guid:g1 | Int64:2
table root_Tags cols=__parent_guid:String,Tags:String
  guid:g1 | String:x
  guid:g1 | String:y

### nested
table root cols=__guid:String,Id:Int64,Info:Dictionary`2,Info_Phone:String,Info_Email:String
  guid:g1 | Int64:1 | {Phone=String:1,Email=String:e} | String:1 | String:e
  guid:g2 | Int64:2 | {Phone=String:2,Email=String:f} | String:2 | String:f
table root_Kids cols=__parent_guid:String,N:String
  guid:g1 | String:a
  guid:g1 | String:b

### primitive-array
table root cols=__guid:String,Id:Int64
  guid:g1 | Int64:1
  guid:g2 | Int64:2
table root_Tags cols=__parent_guid:String,Tags:String
  guid:g1 | String:x
  guid:g1 | String:y
  guid:g1 | String:z
  guid:g2 | String:w

### empty-array
table root cols=

### deep
table root cols=__guid:String,A:Dictionary`2
  guid:g1 | {B=String:{"C":{"D":1}}}
table root_A_B cols=__parent_guid:String,C:Dictionary`2,C_D:Int64
  guid:g1 | {D=Int64:1} | Int64:1
table root_L cols=__parent_guid:String
  guid:g1
table root_L_M cols=__parent_guid:String,O:Int64
  guid:g1 | Int64:1
  guid:g1 | Int64:2

### name-clash
table root cols=__guid:String,a:Dictionary`2,a_b:Int64
  guid:g1 | {b=Int64:1} | Int64:2

### numbers
table root cols=__guid:String,i:Int64,l:Int64,d:Double,e:Double,n:Double
  guid:g1 | Int64:1 | Int64:9999999999 | Double:1.25 | Double:1000 | Double:-0.5

### unicode
table root cols=__guid:String,s:String,t:String
  guid:g1 | String:héllo 😀 | String:tab	here

### array-of-arrays
EXCEPTION InvalidOperationException
### primitive-root
EXCEPTION InvalidOperationException
### trailing-content
EXCEPTION JsonReaderException
### truncated
EXCEPTION JsonReaderException
### array-of-primitives
EXCEPTION InvalidOperationException
""";
    }
}
