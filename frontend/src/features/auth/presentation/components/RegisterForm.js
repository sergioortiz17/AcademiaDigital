import React from 'react';
import { useTranslation } from 'react-i18next';
import { Row, Col, Button, Alert } from 'react-bootstrap';
import * as Yup from 'yup';
import { Formik } from 'formik';
import useScriptRef from '../../../../hooks/useScriptRef';
import { useRegister } from '../../application/useRegister';

const RegisterForm = ({ className, ...rest }) => {
    const { t } = useTranslation();
    const { register, isLoading, error } = useRegister();
    const scriptedRef = useScriptRef();

    return (
        <React.Fragment>
            <Formik
                initialValues={{
                    username: '',
                    email: '',
                    password: '',
                    submit: null
                }}
                validationSchema={Yup.object().shape({
                    email: Yup.string().email(t('auth.validEmail')).max(255).required(t('auth.emailRequired')),
                    username: Yup.string().required(t('auth.usernameRequired')),
                    password: Yup.string().max(255).required(t('auth.passwordRequired'))
                })}
                onSubmit={async (values, { setErrors, setStatus, setSubmitting }) => {
                    try {
                        const result = await register(values);
                        if (result.success) {
                            if (scriptedRef.current) {
                                setStatus({ success: true });
                                setSubmitting(false);
                            }
                        } else {
                            setStatus({ success: false });
                            setErrors({ submit: result.error });
                            setSubmitting(false);
                        }
                    } catch (err) {
                        console.error(err);
                        if (scriptedRef.current) {
                            setStatus({ success: false });
                            setErrors({ submit: err.message });
                            setSubmitting(false);
                        }
                    }
                }}
            >
                {({ errors, handleBlur, handleChange, handleSubmit, isSubmitting, touched, values }) => (
                    <form noValidate onSubmit={handleSubmit} className={className} {...rest}>
                        <div className="form-group mb-3">
                            <input
                                className="form-control"
                                error={touched.username && errors.username}
                                label={t('auth.username')}
                                placeholder={t('auth.username')}
                                name="username"
                                onBlur={handleBlur}
                                onChange={handleChange}
                                type="text"
                                value={values.username}
                            />
                            {touched.username && errors.username && <small className="text-danger form-text">{errors.username}</small>}
                        </div>
                        <div className="form-group mb-3">
                            <input
                                className="form-control"
                                error={touched.email && errors.email}
                                label={t('auth.email')}
                                placeholder={t('auth.email')}
                                name="email"
                                onBlur={handleBlur}
                                onChange={handleChange}
                                type="email"
                                value={values.email}
                            />
                            {touched.email && errors.email && <small className="text-danger form-text">{errors.email}</small>}
                        </div>
                        <div className="form-group mb-4">
                            <input
                                className="form-control"
                                error={touched.password && errors.password}
                                label={t('auth.password')}
                                placeholder={t('auth.password')}
                                name="password"
                                onBlur={handleBlur}
                                onChange={handleChange}
                                type="password"
                                value={values.password}
                            />
                            {touched.password && errors.password && <small className="text-danger form-text">{errors.password}</small>}
                        </div>

                        {(errors.submit || error) && (
                            <Col sm={12}>
                                <Alert variant="danger">{errors.submit || error}</Alert>
                            </Col>
                        )}

                        <div className="custom-control custom-checkbox  text-left mb-4 mt-2">
                            <input type="checkbox" className="custom-control-input" id="customCheck1" />
                            <label className="custom-control-label" htmlFor="customCheck1">
                                {t('auth.saveCredentials')}
                            </label>
                        </div>

                        <Row>
                            <Col mt={2}>
                                <Button
                                    className="btn-block"
                                    color="primary"
                                    disabled={isSubmitting || isLoading}
                                    size="large"
                                    type="submit"
                                    variant="primary"
                                >
                                    {isLoading ? t('common.loading') : t('auth.signUp')}
                                </Button>
                            </Col>
                        </Row>
                    </form>
                )}
            </Formik>
            <hr />
        </React.Fragment>
    );
};

export default RegisterForm;

