public class {class_name} : {base_class_name}
\{{"
"}
{for f in fields:
    private {f.type} {f.name};{"
"}
}

    public override void OnBind()
    \{{"
"}
{for f in fields:

        {f.name} = ControlMap["{f.name}"] as {f.type};{"
"}
}

\}
