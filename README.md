DeJson.NET
==========

A Deserializer/Serializer from JSON to C# classes.

Example:

    using DeJson;

    class Bar {
        public int z;
        public int a;
    }
    class Foo {
        public int x;
        public int y;
        public Bar[] g;
        public int[][] b;
    }

    void Test() {
        // Make
        Deserializer deserializer = new Deserializer();

        string json = "{\"x\":123,\"y\":456,\"g\":[{\"z\":5,\"a\":12},{\"z\":4,\"a\":23},{\"z\":3,\"a\":34}],\"b\":[[1,2],[4,5,6],[7,6]]}";

        Foo f = deserializer.Deserialize<Foo>(json);

        Console.WriteLine("f.x          : " + f.x       );
        Console.WriteLine("f.y          : " + f.y       );
        Console.WriteLine("f.g[0].z     : " + f.g[0].z  );
        Console.WriteLine("f.g[0].a     : " + f.g[0].a  );
        Console.WriteLine("f.g[1].z     : " + f.g[1].z  );
        Console.WriteLine("f.g[1].a     : " + f.g[1].a  );
        Console.WriteLine("f.g[2].z     : " + f.g[2].z  );
        Console.WriteLine("f.g[2].a     : " + f.g[2].a  );
        Console.WriteLine("f.g.Length   : " + f.g.Length);
        Console.WriteLine("f.b[0][0]    : " + f.b[0][0] );
        Console.WriteLine("f.b[0][1]    : " + f.b[0][1] );
        Console.WriteLine("f.b[1][0]    : " + f.b[1][0] );
        Console.WriteLine("f.b[1][1]    : " + f.b[1][1] );
        Console.WriteLine("f.b[1][2]    : " + f.b[1][2] );
        Console.WriteLine("f.b[2][0]    : " + f.b[2][0] );
        Console.WriteLine("f.b[2][1]    : " + f.b[2][1] );
        Console.WriteLine("f.b.Length   : " + f.b.Length);
        Console.WriteLine("f.b[0].Length: " + f.b[0].Length);
        Console.WriteLine("f.b[1].Length: " + f.b[1].Length);
        Console.WriteLine("f.b[2].Length: " + f.b[2].Length);

        // Serialize it.
        Console.WriteLine(Serializer.Serialize(f));
    }

Deserializing Derived Types
---------------------------

If you have some dervied type then you need to provide a CustomCreator to tell
the Deserializer how to decide which derived type to create. Example

    public class Fruit {
      public int fruitType; // 0 = Apple, 1 = Raspberry
    };

    public class Apple : Fruit {
        public float height;
        public float radius;
    };

    public class Raspberry : Fruit {
         public int numBulbs;
    }

    public class FruitCreator : Deserializer.CustomCreator {

        // Called when the Deserializer realizes it needs a Fruit (abstract)
        // so you get a chance to create a concrete instance of a Fruit (Apple, Raspberry)
        public override object Create(Dictionary<string, object> src, Dictionary<string, object> parentSrc) {

            // read the fruitType field
            int fruitType = Convert.ToInt32(src["fruitType"]);
            if (fruitType == 0) {
                return new Apple();
            } else if (fruitType == 1) {
                return new Raspberry();
            }
            return null;
        }

        // Tell the Deserializer the base type we create.
        public override System.Type TypeToCreate() {
            return typeof(Fruit);
        }
    }

...

    void Test() {
        Deserializer deserializer = new Deserializer();
        deserializer.RegisterCreator(new FruitCreator());

        string appleJson = "{\"fruitType\":0,\"height\":1.2,\"radius\":7}";
        string raspberryJson = "{\"fruitType\":1,\"numBulbs\":27}";

        Fruit apple = deserializer.Deserialize<Fruit>(appleJson);
        Fruit raspberry = deserializer.Deserialize<Fruit>(raspberryJson);

        Console.WriteLine("apple.fruitType:     " + apple.fruitType);
        Console.WriteLine("apple.height:        " + ((Apple)apple).height);
        Console.WriteLine("apple.radius:        " + ((Apple)apple).radius);
        Console.WriteLine("raspberry.fruitType: " + raspberry.fruitType);
        Console.WriteLine("raspberry.height:    " + ((Raspberry)raspberry).numBulbs);
    }

If the data to determine the type is in the containing class you can use `parentSrc` to
inspect the parent's field in your creator. Example

    public class Message {
        public string msgType;
        public MessageData data;
    }

    public class MessageData {
    }

    public class MessageDataMouseMove : MessageData {
        public int x;
        public int y;
    }

    public class MessageDataKeyDown : MessageData {
        public int keyCode;
    }

    public class MessageDataCreator : Deserializer.CustomCreator {
        // Called when the Deserializer realizes it needs a
        // MessageData (abstract) so you get a chance to create
        // a concrete instance of a MessageData (MessageDataMouseMove,
        // MessageDataKeyDown)
        public override object Create(Dictionary<string, object> src,
                                      Dictionary<string, object> parentSrc) {

            // read the msgType from Message
            // parentSrc is the fields from Message
            // src is the field from MessageData
            string msgType = Convert.ToString(parentSrc["msgType"]);
            if (msgType.Equals("mouseMove")) {
                return new MessageDataMouseMove();
            } else if (msgType.Equals("keyDown")) {
                return new MessageDataKeyDown();
            }
            return null;
        }

        // Tell the Deserializer the base type we create.
        public override System.Type TypeToCreate() {
            return typeof(MessageData);
        }
    }

...

    void Test() {
        Deserializer deserializer = new Deserializer();
        deserializer.RegisterCreator(new MessageDataCreator());

        string mouseMoveJson = "{\"msgType\":\"mouseMove\",\"data\":{\"x\":123,\"y\":456}}";
        string keyDownJson = "{\"msgType\":\"keyDown\",\"data\":{\"keyCode\":789}}";

        Message msg1 = deserializer.Deserialize<Message>(mouseMoveJson);
        Message msg2 = deserializer.Deserialize<Message>(keyDownJson);

        Console.WriteLine("msg1.msgType                            : " + msg1.msgType                           );
        Console.WriteLine("((MessageDataMouseMove)msg1.data).x     : " + ((MessageDataMouseMove)msg1.data).x    );
        Console.WriteLine("((MessageDataMouseMove)msg1.data).y     : " + ((MessageDataMouseMove)msg1.data).y    );
        Console.WriteLine("msg2.msgType                            : " + msg2.msgType                           );
        Console.WriteLine("((MessageDataKeyDown)msg2.data).keyCode : " + ((MessageDataKeyDown)msg2.data).keyCode);
    }

Notes:
------

I'm a C# noob but it seems to work for the cases I've thrown at it so far.

This was written for consuming JSON provided by JavaScript in a browser

Classes must have a no parameter constructor and all fields must be public.
Consider these classes just for passing info through JSON.

Generics are not supported AFAIK. I'm sure there's a host of other cases
not supported.

Why?
----

I tried to use JSON.NET but I needed it for Unity3D. Unity3D is apparently
using .NET 2.0. JSON.NET supports .NET 2.0 but I couldn't get it to
do. In particular I couldn't get it to handle the 2 advanced cases
above. In JSON.NET you can apparently handle the first case where
the field that determines the type is in the class being deserialized
but unfortunately to do that it uses some advanced features of Linq
that didn't seem to work in Unity3D.

Of course maybe I just don't know what I'm doing and it would have worked
and I could have saved a bunch of time but I couldn't get it to work
and this didn't take too long to write.

License
-------

It's the MIT license. See top of DeJson.cs


To Do:
------

*   Consider updating to handle auto serializing type info.

    If I modified MiniJSON to optionally output type info then I could also
    modify the Deserializer to use that info. This would remove the need
    for `CustomCreator`s for those cases when you're serializing from
    C#

*   Support top level Arrays

    Arrays are supported but they have to be members of a class. In other words

        public class Foo {
           public int[] values;
        };

        Foo foo = deserializer.Deserialize<Foo>(json);

    works but

        int[] values = deserializer.Deserialize<int[]>(json);

    Does not ... Yet

    Array of Arrays are supported. But, being based on JavaScript multi-dimensional arrays
    are not.

        public class Foo {
           public int[][] arrayOfArrayOfValues;  // ok
           public int[,] multiDimensionalArray;  // BAD!
        };

