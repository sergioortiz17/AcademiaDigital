import React, { Suspense, Fragment, lazy } from 'react';
import { Switch, Redirect, Route } from 'react-router-dom';

import Loader from './components/Loader/Loader';
import AdminLayout from './layouts/AdminLayout';

import GuestGuard from './components/Auth/GuestGuard';
import AuthGuard from './features/auth/presentation/components/AuthGuard';

import { BASE_URL } from './config/constant';

export const renderRoutes = (routes = []) => (
    <Suspense fallback={<Loader />}>
        <Switch>
            {routes.map((route, i) => {
                const Guard = route.guard || Fragment;
                const Layout = route.layout || Fragment;
                const Component = route.component;

                return (
                    <Route
                        key={i}
                        path={route.path}
                        exact={route.exact}
                        render={(props) => (
                            <Guard>
                                <Layout>{route.routes ? renderRoutes(route.routes) : <Component {...props} />}</Layout>
                            </Guard>
                        )}
                    />
                );
            })}
        </Switch>
    </Suspense>
);

const routes = [
    {
        exact: true,
        guard: GuestGuard,
        path: '/auth/signin',
        component: lazy(() => import('./views/auth/signin/SignIn1'))
    },
    {
        exact: true,
        guard: GuestGuard,
        path: '/auth/signup',
        component: lazy(() => import('./views/auth/signup/SignUp1'))
    },
    {
        exact: true,
        path: '/auth/signin-2',
        component: lazy(() => import('./views/auth/signin/SignIn2'))
    },
    {
        exact: true,
        path: '/auth/signup-2',
        component: lazy(() => import('./views/auth/signup/SignUp2'))
    },
    {
        path: '*',
        layout: AdminLayout,
        guard: AuthGuard,
        routes: [
            {
                exact: true,
                path: '/app/dashboard/default',
                component: lazy(() => import('./views/dashboard/DashDefault'))
            },
            {
                exact: true,
                path: '/app/courses',
                component: lazy(() => import('./views/courses'))
            },
            {
                exact: true,
                path: '/app/enrollments',
                component: lazy(() => import('./views/enrollments'))
            },
            {
                exact: true,
                path: '/app/calendar',
                component: lazy(() => import('./views/calendar'))
            },
            {
                exact: true,
                path: '/app/teachers',
                component: lazy(() => import('./views/teachers'))
            },
            {
                exact: true,
                path: '/app/certificates',
                component: lazy(() => import('./views/certificates'))
            },
            {
                exact: true,
                path: '/app/grades',
                component: lazy(() => import('./views/grades'))
            },
            {
                exact: true,
                path: '/app/messages',
                component: lazy(() => import('./views/messages'))
            },
            {
                exact: true,
                path: '/app/profile',
                component: lazy(() => import('./views/profile'))
            },
            {
                path: '*',
                exact: true,
                component: () => <Redirect to={BASE_URL} />
            }
        ]
    }
];

export default routes;
