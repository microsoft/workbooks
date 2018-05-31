import { CodeCellResult, CodeCellUpdate } from "../evaluation";
import { ResultRenderer, getFirstRepresentationOfType } from "../rendering";
import * as React from "react";
import { randomReactKey } from "../utils";

// todo: model interface
interface LoginData {
    username: string;
    pwd: string;
}

const RepresentationName = "Xamarin.Interactive.Representations.Login";

// not sure if the result is correct here
export default function LoginRendererFactory(result: CodeCellResult) {
    return getFirstRepresentationOfType(result, RepresentationName)
        ? new LoginRenderer()
        : null;
}

class LoginRenderer implements ResultRenderer {
    // hmm i think this might be wrong
    // what is getRepresentations actually supposed to do o.O
    getRepresentations(result: CodeCellResult) {
        return [
            {
                key: randomReactKey(),
                component: LoginRepresentation,
                componentProps: [],
                displayName: "Test this adam"
            }
        ];
    }
}

class LoginRepresentation extends React.Component<{ loginData: LoginData }> {
    // todo
    // how to expose hooks out to workbook shell for eval?

    render() {
        return (
            <div>
                Login: <input id="login" /> <br />
                Password: <input id="pwd" type="password" /> <br />
                <input type="submit" />
            </div>
        );
    }
}
