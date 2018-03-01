import * as React from 'react';
import { Route } from 'react-router-dom';
import { Layout } from './components/Layout';
import { Home } from './components/Home';
import { FetchData } from './components/FetchData';
import { ComponentPlayground } from './components/ComponentPlayground';

export const routes = <Layout>
    <Route exact path='/' component={ Home } />
    <Route path='/component-playground' component={ ComponentPlayground } />
</Layout>;