import * as React from 'react';
import { Route, Link, RouteComponentProps, RouteProps, match } from 'react-router-dom';
import { Layout, LayoutProps } from './components/Layout';
import { ImageFit } from 'office-ui-fabric-react/lib/Image'
import { Home } from './components/Home';
import { ComponentPlayground } from './components/ComponentPlayground';
import { WorkbookShell } from './components/WorkbookShell';
import { DocumentCard, DocumentCardTitle, DocumentCardLogo, DocumentCardPreview } from 'office-ui-fabric-react/lib/DocumentCard';
import { createLocation } from "history";

export interface CardProps extends RouteProps {
    cardId: string,
}

export interface CardCatalog {
    cardId: string
    title: string
    icon?: string
    contentString?: string
    contentUrl?: string
}

export class CardItem extends React.Component<RouteComponentProps<CardProps>, {}> {
    getProps(cardId: string): any {
        var match = Catalog.Cards.find((card) => {
            return card.cardId === cardId
        })

        if (match === undefined)
            return {}

        return {
            contentUrl: match.contentUrl,
            contentString: match.contentString
        }
    }

    public render() {
        return <WorkbookShell {...this.getProps(this.props.match.params.cardId)} />
    }
}
export class Catalog extends React.Component<RouteComponentProps<CardProps>, { cards: CardCatalog[] }> {
    static loaded = false
    constructor() {
        super();
        this.state = { cards: Catalog.Cards }
    }

    public componentDidMount() {
        var t = fetch('/api/workbook')
            .then(response => response.json())
            .then((newCards) => {
                if (newCards) {
                    Catalog.Cards = newCards.map((card: CardCatalog, key: any) => {
                        //var old = Catalog.getCatalog(card.cardId)
                        return { ...card }
                })
            }
            this.setState ({cards: newCards})
        })
    }

    public static Cards: CardCatalog [] = []

    public render() {
        return <div>
        <h2>Workbooks</h2>
        <div className="ms-Grid">
            <div className="ms-Grid-row">
                {this.state.cards.map((card, key) => {
                    const location = createLocation(`/live/${card.cardId}`, null, undefined, this.props.history.location)
                    const url = this.props.history.createHref (location)
                    const previewProps = {
                        previewImages: [
                            {
                                width: 200,
                                height: 200,
                                previewImageSrc: card.icon || 'Icon.png',
                                imageFit: ImageFit.contain
                            }
                        ]
                    };
                        
                    return (
                        <div className="ms-Grid-col ms-sm6 ms-md3 ms-lg3" key={key}>
                            <DocumentCard onClick={() => { this.props.history.push(url)}}>
                                <DocumentCardPreview {...previewProps} />
                                <DocumentCardTitle title={card.title} />
                            </DocumentCard>
                        </div>
                    )
                })}
            </div>
        </div>
        <Route exact path={this.props.match.url} render={() => <h3>Select a workbook to begin</h3>} />
    </div>
    }
}

export const routes = <Layout>
    <Route exact path='/' component={ Catalog } />
    <Route path='/live/:cardId' component={ CardItem } />
    <Route path='/component-playground' component={ ComponentPlayground } />
</Layout>