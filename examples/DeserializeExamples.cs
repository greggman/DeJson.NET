/*
 * Copyright 2014, Gregg Tavares.
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are
 * met:
 *
 *     * Redistributions of source code must retain the above copyright
 * notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above
 * copyright notice, this list of conditions and the following disclaimer
 * in the documentation and/or other materials provided with the
 * distribution.
 *     * Neither the name of Gregg Tavares. nor the names of its
 * contributors may be used to endorse or promote products derived from
 * this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
 * "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
 * LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
 * A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT
 * OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
 * LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
 * DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
 * THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
 * OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

/**
   Note: Since I wrote this libray for Unity3D work this example
   is for Unity3D. Add as script to some GameObject, look in the
   console to see the results.
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MiniJSON;
using DeJson;
using System;

public class DeserializeExamples : MonoBehaviour {

    // ----------------------------------------------
    // Classes for basic example
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

    // ----------------------------------------------
    // Classes for Dervied example #1

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
        public override object Create(Dictionary<string, object> src,
                                      Dictionary<string, object> parentSrc) {

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

    // ----------------------------------------------
    // Classes for Derived example #2

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

    // ----------------------------------------------
    // Classes for more advanced example.
    //
    // This is basically the same as Derived sample #2 except with
    // a few fancier classes to make adding new MessageCmdData derived
    // classes really easy.

    public class MessageCmdData {
    };

    public class MessageCmd {
        public string cmd;
        public MessageCmdData data;
    };

    public class MessageToClient {
        public string cmd;  // command 'server', 'update'
        public int id;      // id of client
        public MessageCmd data;
    };

    [CmdName("setColor")]
    public class MessageSetColor : MessageCmdData {
        public string color;
        public string style;
    };

    [CmdName("setName")]
    public class MessageSetName : MessageCmdData {
        public string name;
    };

    [CmdName("launch")]
    public class MessageLaunch : MessageCmdData {
    };

    [CmdName("die")]
    public class MessageDie : MessageCmdData {
        public string killer;
        public bool crash;
    };

    [AttributeUsage(AttributeTargets.Class)]
    public class CmdNameAttribute : System.Attribute
    {
       public readonly string CmdName;

       public CmdNameAttribute(string cmdName)
       {
          this.CmdName = cmdName;
       }
    }

    public class MessageCmdDataCreator : Deserializer.CustomCreator {

        public abstract class Creator {
            public abstract object Create();
        }

        public class TypedCreator<T> : Creator where T : new()  {
            public override object Create() {
                return new T();
            }
        }

        public MessageCmdDataCreator() {
            m_creators = new Dictionary<string, Creator>();
        }

        public void RegisterCreator<T>() where T : new() {
            Type type = typeof(T);
            string name = null;

            //Querying Class Attributes
            foreach (Attribute attr in type.GetCustomAttributes(true)) {
                CmdNameAttribute cmdNameAttr = attr as CmdNameAttribute;
                if (cmdNameAttr != null) {
                    name = cmdNameAttr.CmdName;
                }
            }

            if (name == null) {
                System.InvalidOperationException ex = new System.InvalidOperationException("missing CmdNameAttribute");
                throw ex;
            }

            m_creators[name] = new TypedCreator<T>();
        }

        public override object Create(Dictionary<string, object> src, Dictionary<string, object> parentSrc) {
            string typeName = (string)parentSrc["cmd"];
            Creator creator;
            if (m_creators.TryGetValue(typeName, out creator)) {
                return creator.Create();
            }
            return null;
        }

        public override System.Type TypeToCreate() {
            return typeof(MessageCmdData);
        }

        Dictionary<string, Creator> m_creators;
    };


    // Use this for initialization
    void Start () {

        // ----------------------------------------------
        // Basic example

        Deserializer deserializer = new Deserializer();

        string json = "{\"x\":123,\"y\":456,\"g\":[{\"z\":5,\"a\":12},{\"z\":4,\"a\":23},{\"z\":3,\"a\":34}],\"b\":[[1,2],[4,5,6],[7,6]]}";

        Foo f = deserializer.Deserialize<Foo>(json);

        print("--[ basic-example ]------------------------");
        print("f.x          : " + f.x       );
        print("f.y          : " + f.y       );
        print("f.g[0].z     : " + f.g[0].z  );
        print("f.g[0].a     : " + f.g[0].a  );
        print("f.g[1].z     : " + f.g[1].z  );
        print("f.g[1].a     : " + f.g[1].a  );
        print("f.g[2].z     : " + f.g[2].z  );
        print("f.g[2].a     : " + f.g[2].a  );
        print("f.g.Length   : " + f.g.Length);
        print("f.b[0][0]    : " + f.b[0][0] );
        print("f.b[0][1]    : " + f.b[0][1] );
        print("f.b[1][0]    : " + f.b[1][0] );
        print("f.b[1][1]    : " + f.b[1][1] );
        print("f.b[1][2]    : " + f.b[1][2] );
        print("f.b[2][0]    : " + f.b[2][0] );
        print("f.b[2][1]    : " + f.b[2][1] );
        print("f.b.Length   : " + f.b.Length);
        print("f.b[0].Length: " + f.b[0].Length);
        print("f.b[1].Length: " + f.b[1].Length);
        print("f.b[2].Length: " + f.b[2].Length);

        print(Serializer.Serialize(f));

        // ----------------------------------------------
        // Array

        string kj = "[4,7,9]";

        int[] k = deserializer.Deserialize<int[]>(kj);

        print("k[0]: " + k[0]);
        print("k[1]: " + k[1]);
        print("k[2]: " + k[2]);

        print(Serializer.Serialize(k));

        // ----------------------------------------------
        // Derived Classes example #1

        deserializer.RegisterCreator(new FruitCreator());

        string appleJson = "{\"fruitType\":0,\"height\":1.2,\"radius\":7}";
        string raspberryJson = "{\"fruitType\":1,\"numBulbs\":27}";

        Fruit apple = deserializer.Deserialize<Fruit>(appleJson);
        Fruit raspberry = deserializer.Deserialize<Fruit>(raspberryJson);

        print("--[ dervied classes example #1 ]------------------------");
        print("apple.fruitType:     " + apple.fruitType);
        print("apple.height:        " + ((Apple)apple).height);
        print("apple.radius:        " + ((Apple)apple).radius);
        print("raspberry.fruitType: " + raspberry.fruitType);
        print("raspberry.height:    " + ((Raspberry)raspberry).numBulbs);

        print(Serializer.Serialize(apple));
        print(Serializer.Serialize(raspberry));

        // ----------------------------------------------
        // Derived Classes example #2

        deserializer.RegisterCreator(new MessageDataCreator());

        string mouseMoveJson = "{\"msgType\":\"mouseMove\",\"data\":{\"x\":123,\"y\":456}}";
        string keyDownJson = "{\"msgType\":\"keyDown\",\"data\":{\"keyCode\":789}}";

        Message msg1 = deserializer.Deserialize<Message>(mouseMoveJson);
        Message msg2 = deserializer.Deserialize<Message>(keyDownJson);

        print("--[ dervied classes example #2 ]------------------------");
        print("msg1.msgType                            : " + msg1.msgType                           );
        print("((MessageDataMouseMove)msg1.data).x     : " + ((MessageDataMouseMove)msg1.data).x    );
        print("((MessageDataMouseMove)msg1.data).y     : " + ((MessageDataMouseMove)msg1.data).y    );
        print("msg2.msgType                            : " + msg2.msgType                           );
        print("((MessageDataKeyDown)msg2.data).keyCode : " + ((MessageDataKeyDown)msg2.data).keyCode);

        print(Serializer.Serialize(msg1));
        print(Serializer.Serialize(msg2));

        // ----------------------------------------------
        // More advanced example

        print("--[ More advanced example ]------------------------");

        MessageCmdDataCreator mcdc = new MessageCmdDataCreator();

        mcdc.RegisterCreator<MessageSetColor>();
        mcdc.RegisterCreator<MessageSetName>();
        mcdc.RegisterCreator<MessageLaunch>();
        mcdc.RegisterCreator<MessageDie>();

        deserializer.RegisterCreator(mcdc);

        string ja = "{\"cmd\":\"update\",\"id\":123,\"data\":{\"cmd\":\"setColor\",\"data\":{\"color\":\"red\",\"style\":\"bold\"}}}";
        string jb = "{\"cmd\":\"update\",\"id\":345,\"data\":{\"cmd\":\"setName\",\"data\":{\"name\":\"gregg\"}}}";
        string jc = "{\"cmd\":\"update\",\"id\":789,\"data\":{\"cmd\":\"launch\",\"data\":{}}}";
        string jd = "{\"cmd\":\"update\",\"id\":101112,\"data\":{\"cmd\":\"die\",\"data\":{\"killer\":\"jill\",\"crash\":true}}}";

        MessageToClient ma = deserializer.Deserialize<MessageToClient>(ja);
        MessageToClient mb = deserializer.Deserialize<MessageToClient>(jb);
        MessageToClient mc = deserializer.Deserialize<MessageToClient>(jc);
        MessageToClient md = deserializer.Deserialize<MessageToClient>(jd);

        print("ma.cmd                                : " + ma.cmd                               );
        print("ma.id                                 : " + ma.id                                );
        print("ma.data.cmd                           : " + ma.data.cmd                          );
        print("((MessageSetColor)ma.data.data).color : " + ((MessageSetColor)ma.data.data).color);
        print("((MessageSetColor)ma.data.data).style : " + ((MessageSetColor)ma.data.data).style);

        print("mb.cmd                                : " + mb.cmd                               );
        print("mb.id                                 : " + mb.id                                );
        print("mb.data.cmd                           : " + mb.data.cmd                          );
        print("((MessageSetName)mb.data.data).name   : " + ((MessageSetName)mb.data.data).name  );

        print("mc.cmd                                : " + mc.cmd                               );
        print("mc.id                                 : " + mc.id                                );
        print("mc.data.cmd                           : " + mc.data.cmd                          );

        print("md.cmd                                : " + md.cmd                               );
        print("md.id                                 : " + md.id                                );
        print("md.data.cmd                           : " + md.data.cmd                          );
        print("((MessageDie)md.data.data).killer     : " + ((MessageDie)md.data.data).killer    );
        print("((MessageDie)md.data.data).crash      : " + ((MessageDie)md.data.data).crash     );

        print(Serializer.Serialize(ma));
        print(Serializer.Serialize(mb));
        print(Serializer.Serialize(mc));
        print(Serializer.Serialize(md));
    }
}
