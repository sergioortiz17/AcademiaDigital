import React from 'react';
import { useTranslation } from 'react-i18next';
import { Card, Row, Col } from 'react-bootstrap';
import { NavLink } from 'react-router-dom';

import RegisterForm from '../../../features/auth/presentation/components/RegisterForm';
import Breadcrumb from '../../../layouts/AdminLayout/Breadcrumb';

const SignUp1 = () => {
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
                    <Card className="borderless">
                        <Row className="align-items-center">
                            <Col>
                                <Card.Body className="text-center">
                                    <h4 className="mb-4">{t('auth.appName')}</h4>

                                    <div className="mb-4">
                                        <i className="feather icon-user-plus auth-icon" />
                                    </div>

                                    <RegisterForm />

                                    <p className="mb-2">
                                        {t('auth.alreadyHaveAccount')}{' '}
                                        <NavLink to="/auth/signin" className="f-w-400">
                                            {t('auth.signIn')}
                                        </NavLink>
                                    </p>

                                    <br />

                                </Card.Body>
                            </Col>
                        </Row>
                    </Card>
                </div>
            </div>
        </React.Fragment>
    );
};

export default SignUp1;
