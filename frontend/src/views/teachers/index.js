import React from 'react';
import { useTranslation } from 'react-i18next';
import { Card, Row, Col } from 'react-bootstrap';

const Teachers = () => {
    const { t } = useTranslation();

    return (
        <React.Fragment>
            <Row>
                <Col>
                    <Card>
                        <Card.Header>
                            <h5>{t('pages.teachers.title')}</h5>
                        </Card.Header>
                        <Card.Body>
                            <p>{t('pages.teachers.description')}</p>
                            <p className="text-muted">{t('pages.teachers.comingSoon')}</p>
                        </Card.Body>
                    </Card>
                </Col>
            </Row>
        </React.Fragment>
    );
};

export default Teachers;

