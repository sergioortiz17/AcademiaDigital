import React from 'react';
import { useTranslation } from 'react-i18next';
import { Card, Row, Col } from 'react-bootstrap';

const Enrollments = () => {
    const { t } = useTranslation();

    return (
        <React.Fragment>
            <Row>
                <Col>
                    <Card>
                        <Card.Header>
                            <h5>{t('pages.enrollments.title')}</h5>
                        </Card.Header>
                        <Card.Body>
                            <p>{t('pages.enrollments.description')}</p>
                            <p className="text-muted">{t('pages.enrollments.comingSoon')}</p>
                        </Card.Body>
                    </Card>
                </Col>
            </Row>
        </React.Fragment>
    );
};

export default Enrollments;

