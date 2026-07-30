    public class {class_name} : GameUILogic
    \{{"
"}
{for f in fields:
{"        "}{f.type} {f.name};{"
"}
}

        public override void OnBind()
        \{{"
"}
{for f in fields:

{"           "}{f.name} = controlMap["{f.name}"] as {f.type};{"
"}
}

        \}
    \}