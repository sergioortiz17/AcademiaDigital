import React from 'react';
import { useTranslation } from 'react-i18next';
import { Card } from 'react-bootstrap';
import { NavLink } from 'react-router-dom';

import Breadcrumb from '../../../layouts/AdminLayout/Breadcrumb';

import LoginForm from '../../../features/auth/presentation/components/LoginForm';

const Signin1 = () => {
    const { t } = useTranslation();
    
    return (
        <React.Fragment>
            <Breadcrumb />
            <div className="auth-wrapper">
                <div className="auth-content">
                    <div className="auth-bg">
                        <span className="r" />
                        <span className="r s" />
                        <span className="r s" />
                        <span className="r" />
                    </div>
                    <Card className="borderless text-center">
                        <Card.Body>
                            <h4 className="mb-4">{t('auth.appName')}</h4>

                            <div className="mb-4">
                                <i className="feather icon-unlock auth-icon" />
                            </div>

                            <LoginForm />

                            <p className="mb-0 text-muted">
                                {t('auth.dontHaveAccount')}{' '}
                                <NavLink to="/auth/signup" className="f-w-400">
                                    {t('auth.signUp')}
                                </NavLink>
                            </p>

                            <br />

                        </Card.Body>
                    </Card>
                </div>
            </div>
        </React.Fragment>
    );
};

export default Signin1;
