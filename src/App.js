import React from 'react';
import 'bootstrap/dist/css/bootstrap.min.css';
import './App.scss';
import { Container, Row, Col, Label, Button, Input, Form, FormGroup } from 'reactstrap';
import Select from 'react-select';
import { get, set } from 'lodash';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faTrash, faSquare, faExchangeAlt } from '@fortawesome/free-solid-svg-icons'

const XLSX = window.require('xlsx');
const fs = window.require('fs');
const path = window.require('path');
const { dialog } = window.require("electron").remote;

const supportedExts = ['.xlsx', '.xls'];

const fieldNames = {
  'A': 'Số tờ khai',
  'B': 'Mã Chi cục Hải quan tạo mới',
  'C': 'Mã phân loại trạng thái sau cùng',
  'D': 'Bộ phận kiểm tra hồ sơ đầu tiên',
  'E': 'Bộ phận kiểm tra hồ sơ sau cùng',
  'F': 'Phương thức vận chuyển',
  'G': 'Mã loại hình',
  'H': 'Ngày đăng ký',
  'I': 'Giờ đăng ký',
  'J': 'Ngày thay đổi đăng ký',
  'K': 'Giờ thay đổi đăng ký',
  'L': 'Mã người nhập khẩu',
  'M': 'Tên người nhập khẩu',
  'N': 'Số điện thoại người nhập khẩu',
  'O': 'Tên người ủy thác nhập khẩu',
  'P': 'Tên người xuất khẩu',
  'Q': 'Mã nước(Country, coded)',
  'R': 'Số vận đơn (Số B/L, số AWB v.v. …) 1',
  'S': 'Số vận đơn (Số B/L, số AWB v.v. …) 2',
  'T': 'Số vận đơn (Số B/L, số AWB v.v. …) 3',
  'U': 'Số vận đơn (Số B/L, số AWB v.v. …) 4',
  'V': 'Số vận đơn (Số B/L, số AWB v.v. …) 5',
  'W': 'Số lượng',
  'X': 'Mã đơn vị tính',
  'Y': 'Tổng trọng lượng hàng (Gross)',
  'Z': 'Mã đơn vị tính trọng lượng (Gross)',
  'AA': 'Số lượng container',
  'AB': 'Mã địa điểm dỡ hàng',
  'AC': 'Mã địa điểm xếp hàng',
  'AD': 'Tên phương tiện vận chuyển',
  'AE': 'Ngày hàng đến',
  'AF': 'Phương thức thanh toán',
  'AG': 'Tổng trị giá hóa đơn',
  'AH': 'Tổng trị giá tính thuế',
  'AI': 'Tổng tiền thuế phải nộp',
  'AJ': 'Tổng số dòng hàng của tờ khai',
  'AK': 'Ngày cấp phép',
  'AL': 'Giờ cấp phép',
  'AM': 'Ngày hoàn thành kiểm tra',
  'AN': 'Giờ hoàn thành kiểm tra',
  'AO': 'Ngày hủy khai báo Hải quan',
  'AP': 'Giờ hủy khai báo Hải quan',
  'AQ': 'Tên người phụ trách kiểm tra hồ sơ',
  'AR': 'Tên người phụ trách kiểm hóa',
  'AS': 'Mã số hàng hóa',
  'AT': 'Mô tả hàng hóa',
  'AU': 'Số lượng (1)',
  'AV': 'Mã đơn vị tính (1)',
  'AW': 'Trị giá hóa đơn',
  'AX': 'Đơn giá hóa đơn',
  'AY': 'Mã đồng tiền của đơn giá',
  'AZ': 'Đơn vị của đơn giá và số lượng',
  'BA': 'Trị giá tính thuế(S)',
  'BB': 'Trị giá tính thuế(M)',
  'BC': 'Đơn giá tính thuế',
  'BD': 'Thuế suất thuế nhập khẩu',
  'BE': 'Số tiền thuế nhập khẩu',
  'BF': 'Mã nước xuất xứ'
};

const fieldNameToOption = name => ({
  value: name,
  label: fieldNames[name],
  key: name
});

const fieldNamesKeys = [
  'A',
  'B',
  'C',
  'D',
  'E',
  'F',
  'G',
  'H',
  'I',
  'J',
  'K',
  'L',
  'M',
  'N',
  'O',
  'P',
  'Q',
  'R',
  'S',
  'T',
  'U',
  'V',
  'W',
  'X',
  'Y',
  'Z',
  'AA',
  'AB',
  'AC',
  'AD',
  'AE',
  'AF',
  'AG',
  'AH',
  'AI',
  'AJ',
  'AK',
  'AL',
  'AM',
  'AN',
  'AO',
  'AP',
  'AQ',
  'AR',
  'AS',
  'AT',
  'AU',
  'AV',
  'AW',
  'AX',
  'AY',
  'AZ',
  'BA',
  'BB',
  'BC',
  'BD',
  'BE',
  'BF'
]

const fieldOptions = fieldNamesKeys.map(fieldNameToOption);

const CONDITION_TYPE = {
  AND_GROUP: 'AND_GROUP',
  OR_GROUP: 'OR_GROUP',
  MATCH_GROUP: 'MATCH_GROUP'
}

const MATCH_TYPE = {
  CONTAIN: 'CONTAIN',
  EQUAL: 'EQUAL',
  START_WITH: 'START_WITH'
}

const matchTypeToOption = name => ({
  value: name,
  label: name,
  key: name
});

const matchTypeOptions = Object.keys(MATCH_TYPE).map(key => matchTypeToOption(MATCH_TYPE[key]));

export default class App extends React.Component {
  state = {
    // inputDirectory: 'E:\\Work\\Dan\\filter-excel-test\\Input',
    // outputDirectory: 'E:\\Work\\Dan\\filter-excel-test\\Output',
    inputDirectory: '',
    outputDirectory: '',
    conditions: {
      type: CONDITION_TYPE.AND_GROUP,
      children: [
        {
          type: CONDITION_TYPE.MATCH_GROUP,
          fieldName: 'A',
          matchType: MATCH_TYPE.CONTAIN,
          value: ''
        }
      ]
    },
    folderSize: 0,
    rowCount: 0,
    matchingRows: 0,
    errors: [],
    currentFile: '',
  }

  composeOnDirectoryChange = name => async () => {
    var folderPath = dialog.showOpenDialogSync({
      properties: ['openDirectory']
    });

    if (!folderPath || !folderPath.length) return;

    this.setState({
      [name]: folderPath[0]
    })
  }

  filter = async () => {
    const { inputDirectory, outputDirectory } = this.state;
    if (!inputDirectory || !outputDirectory) return;

    const matchingRows = [];
    const errors = [];

    this.setState({
      matchingRows: matchingRows,
      errors,
      folderSize: 0
    });

    let goAhead = true;
    const files = await fs.promises.readdir(outputDirectory);

    if (files.length > 0) {
      goAhead = window.confirm("Output folder is not empty. This will delete all files currently in output folder.\nAre you sure?");
      if (goAhead) {
        try {
          for (const file of files) {
            await fs.promises.unlink(path.join(outputDirectory, file));
          }
        }
        catch (error) {
          console.error(error);
          alert('ERROR!\nAn error has occured while emptying folder.\nMaybe one of the files are being used by another program?');
          this.setState({
            filtering: false
          });
          return;
        }
      }
    }

    if (!goAhead) {
      this.setState({ filtering: false });
      return;
    }

    const evaluator = eval(`obj => ${this.createEvaluateFunction(this.state.conditions)}`);


    const folderContents = await fs.promises.readdir(inputDirectory);

    let rowCount = 0;
    let matchCount = 0;
    for (const fileName of folderContents) {
      await new Promise(resolve => this.setState({ currentFile: fileName }, () => setTimeout(resolve, 200)));

      const matches = [];
      try {
        const filePath = `${inputDirectory}/${fileName}`;
        if (!this.isExcelFile(filePath)) continue;
        const fileContent = await this.parseExcelFile(filePath);

        rowCount += fileContent.length;
        for (const content of fileContent) {
          if (evaluator(content)) {
            matches.push(content);
          }
        }

        matchCount += matches.length;

        const resultWorkbook = XLSX.utils.book_new();
        var ws = XLSX.utils.json_to_sheet(
          [fieldNames, ...matches],
          { header: fieldNamesKeys, skipHeader: true }
        );
        XLSX.utils.book_append_sheet(resultWorkbook, ws, "Sheet1");
        XLSX.writeFile(resultWorkbook, `${outputDirectory}/${fileName}`);
      } catch (err) {
        console.error(err);
        errors.push(err.toString());
      }
    }

    this.setState({
      currentFile: '',
      matchingRows: matchCount,
      rowCount: rowCount,
      errors,
      folderSize: folderContents.length
    });
  }

  isExcelFile = fullPath => !fs.statSync(fullPath).isDirectory() && supportedExts.includes(path.extname(fullPath))

  parseExcelFile = filePath => new Promise((resolve, reject) => {
    const workbook = XLSX.readFile(filePath);
    const results = [];
    try {
      const sheet = workbook.Sheets[workbook.SheetNames[0]];
      const rowNum = parseInt(sheet['!ref'].split(':')[1].replace('BF', ''));
      console.log(rowNum);
      for (let i = 2; i <= rowNum; i++) {
        results.push(
          fieldNamesKeys.reduce((sum, val) => ({
            ...sum,
            [val]: (sheet[`${val}${i}`] || { v: '' }).v.toString().trim()
          }), {})
        )
      }
    } catch (err) {
      console.error(err);
      reject(`File ${filePath} cannot be parsed!`);
    }

    resolve(results);
  })

  createEvaluateFunction = (condition) => {
    if (condition.type === CONDITION_TYPE.MATCH_GROUP) {
      if (!condition.fieldName || !condition.matchType) return 'true';
      return `obj["${condition.fieldName}"]${this.operantFromMatchType(condition.matchType, condition.value)}`;
    }
    else if (condition.type === CONDITION_TYPE.AND_GROUP) {
      return `(${condition.children.map(child => this.createEvaluateFunction(child)).join(' && ')})`;
    }
    else if (condition.type === CONDITION_TYPE.OR_GROUP) {
      return `(${condition.children.map(child => this.createEvaluateFunction(child)).join(' || ')})`;
    }
  }

  operantFromMatchType = (matchType, value) => {
    switch (matchType) {
      case MATCH_TYPE.EQUAL: return ` == "${value}"`;
      case MATCH_TYPE.START_WITH: return `.startsWith("${value}")`;
      case MATCH_TYPE.CONTAIN:
      default:
        return `.includes("${value}")`;
    }
  }

  renderCondition = (condition, objectPath = '') => {
    switch (condition.type) {
      case CONDITION_TYPE.MATCH_GROUP: return this.renderMatchGroup(condition, objectPath);
      case CONDITION_TYPE.OR_GROUP: return this.renderOrGroup(condition, objectPath);
      case CONDITION_TYPE.AND_GROUP:
      default:
        return this.renderAndGroup(condition, objectPath);
    }
  }

  renderAndGroup = (condition, objectPath) => {
    return (
      <div className="condition_group container_group border border-primary" key={objectPath}>
        <div className="d-flex justify-content-between" style={{ marginLeft: -30 }}>
          <h5 className="text-primary">AND GROUP</h5>
          <div>
            {this.renderSwapTypeButton(objectPath)}
            {objectPath && this.renderDeleteButton(objectPath)}
          </div>
        </div>
        {
          condition.children
            .map((child, index) => (
              <div key={index}>
                {index > 0 && <div className="text-center text-primary">AND</div>}
                {this.renderCondition(child, `${objectPath ? `${objectPath}.` : ''}children[${index}]`)}
              </div>
            ))
        }
        {this.renderAddButtons(objectPath)}
      </div>
    )
  }

  renderOrGroup = (condition, objectPath) => {
    return (
      <div className="condition_group container_group border border-warning" key={objectPath}>
        <div className="d-flex justify-content-between" style={{ marginLeft: -30 }}>
          <h5 className="text-warning">OR GROUP</h5>
          <div>
            {this.renderSwapTypeButton(objectPath)}
            {objectPath && this.renderDeleteButton(objectPath)}
          </div>
        </div>
        {
          condition.children.map((child, index) => (
            <div key={index}>
              {index > 0 && <div className="text-center text-warning">OR</div>}
              {this.renderCondition(child, `${objectPath ? `${objectPath}.` : ''}children[${index}]`)}
            </div>
          ))
        }
        {this.renderAddButtons(objectPath)}
      </div>
    )
  }

  renderMatchGroup = (condition, objectPath) => {
    return (
      <div className="condition_group border d-flex border-light" key={objectPath}>
        <Select
          style={{
            flex: 1
          }}
          styles={{
            container: (provided, state) => ({
              ...provided,
              flex: 2
            }),
            option: (provided, state) => ({
              ...provided,
              color: '#333333'
            })
          }}
          value={fieldNameToOption(condition.fieldName)}
          onChange={this.composeOnFieldNameChanged(objectPath)}
          options={fieldOptions}
        />
        <Select
          style={{
            flex: 1
          }}
          styles={{
            container: (provided, state) => ({
              ...provided,
              flex: '1 0 auto',
              margin: '0 30px'
            }),
            option: (provided, state) => ({
              ...provided,
              color: '#333333'
            })
          }}
          value={matchTypeToOption(condition.matchType)}
          onChange={this.composeOnMatchTypeChanged(objectPath)}
          options={matchTypeOptions}
        />
        <Input
          style={{
            flex: 2
          }}
          type="text"
          value={condition.value}
          onChange={this.composeOnMatchValueChanged(objectPath)}
        />
        {this.renderDeleteButton(objectPath)}
      </div>
    )
  }

  renderDeleteButton = objectPath => (
    <Button color="outline-danger" className="ml-3" onClick={this.composeDeleteGroup(objectPath)}>
      <FontAwesomeIcon icon={faTrash} />
    </Button>
  );

  renderSwapTypeButton = objectPath => (
    <Button color="outline-secondary" onClick={this.composeSwapGroupType(objectPath)}>
      <FontAwesomeIcon icon={faSquare} className="text-primary" />
      {' '}<FontAwesomeIcon icon={faExchangeAlt} />{' '}
      <FontAwesomeIcon icon={faSquare} className="text-warning" />
    </Button>
  )

  renderAddButtons = objectPath => (
    <div className="condition_group border border-dark d-flex justify-content-center">
      <Button
        color="outline-primary"
        className="mx-3"
        onClick={this.composeAddGroup(objectPath, CONDITION_TYPE.AND_GROUP)}
        title="Add AND Group"
      >
        + <FontAwesomeIcon icon={faSquare} />
      </Button>
      <Button
        color="outline-warning"
        className="mx-3"
        onClick={this.composeAddGroup(objectPath, CONDITION_TYPE.OR_GROUP)}
        title="Add OR Group"
      >
        + <FontAwesomeIcon icon={faSquare} />
      </Button>
      <Button
        color="outline-light"
        className="mx-3"
        onClick={this.composeAddGroup(objectPath, CONDITION_TYPE.MATCH_GROUP)}
        title="Add MATCH Group"
      >
        + <FontAwesomeIcon icon={faSquare} />
      </Button>
    </div>
  )

  updateConditions = (objectPath, newValue) => {
    if (!objectPath) {
      this.setState({ conditions: newValue });
    }
    else {
      const conditions = {
        ...this.state.conditions
      }
      set(conditions, objectPath, newValue);
      this.setState({ conditions });
    }
  }

  composeOnFieldNameChanged = objectPath => option => {
    const value = get(this.state.conditions, objectPath);
    value.fieldName = option.value;
    this.updateConditions(objectPath, value);
  }

  composeOnMatchTypeChanged = objectPath => option => {
    const value = get(this.state.conditions, objectPath);
    value.matchType = option.value;
    this.updateConditions(objectPath, value);
  }

  composeOnMatchValueChanged = objectPath => e => {
    const value = get(this.state.conditions, objectPath);
    value.value = e.target.value;
    this.updateConditions(objectPath, value);
  }

  composeSwapGroupType = objectPath => () => {
    const value = objectPath ? get(this.state.conditions, objectPath) : this.state.conditions;
    value.type = value.type === CONDITION_TYPE.AND_GROUP ? CONDITION_TYPE.OR_GROUP : CONDITION_TYPE.AND_GROUP;
    this.updateConditions(objectPath, value);
  }

  composeAddGroup = (objectPath, type) => () => {
    const value = objectPath ? get(this.state.conditions, objectPath) : this.state.conditions;
    switch (type) {
      case CONDITION_TYPE.MATCH_GROUP:
        value.children.push({
          type: CONDITION_TYPE.MATCH_GROUP,
          fieldName: null,
          matchType: MATCH_TYPE.CONTAIN,
          value: ''
        })
        break;
      case CONDITION_TYPE.OR_GROUP:
        value.children.push({
          type: CONDITION_TYPE.OR_GROUP,
          children: []
        })
        break;
      case CONDITION_TYPE.AND_GROUP:
      default:
        value.children.push({
          type: CONDITION_TYPE.AND_GROUP,
          children: []
        })
        break;
    }
    this.updateConditions(objectPath, value);
  }

  composeDeleteGroup = objectPath => () => {
    const conditions = { ...this.state.conditions };
    const lastIndex = objectPath.lastIndexOf('[');
    const parentPath = objectPath.substring(0, lastIndex);
    const index = parseInt(objectPath.substring(lastIndex + 1));

    const parent = get(conditions, parentPath);
    const newParent = parent.filter((item, i) => i !== index);
    set(conditions, parentPath, newParent);
    this.setState({ conditions });
  }

  render() {
    const { inputDirectory, outputDirectory, currentFile, conditions } = this.state;

    return (
      <Container fluid className="py-5">
        <Form>
          <Row>
            <Col>
              <FormGroup style={{ position: 'relative' }}>
                <Label>Input Folder</Label>
                <Input
                  type="text"
                  className="btn-block"
                  style={{ cursor: 'pointer' }}
                  readOnly
                  value={this.state.inputDirectory}
                  placeholder="Click to select..."
                  onClick={this.composeOnDirectoryChange('inputDirectory')}
                />
              </FormGroup>
            </Col>
            <Col>
              <FormGroup style={{ position: 'relative' }}>
                <Label>Output Folder</Label>
                <Input
                  type="text"
                  className="btn-block"
                  style={{ cursor: 'pointer' }}
                  readOnly
                  value={this.state.outputDirectory}
                  placeholder="Click to select..."
                  onClick={this.composeOnDirectoryChange('outputDirectory')}
                />
              </FormGroup>
            </Col>
          </Row>
        </Form>
        <hr />
        <h2>Filters</h2>
        {
          conditions && this.renderCondition(conditions)
        }
        <hr />

        {
          currentFile
            ? (<div>Processing file <code>{currentFile}</code>, please wait...</div>)
            : (
              <Button
                color="success"
                className="btn-block"
                onClick={this.filter}
                disabled={!inputDirectory || !outputDirectory || !!currentFile}
              >
                Start Filtering
              </Button>
            )
        }
        {
          this.state.errors && (
            <ul className="text-danger mt-5">
              {this.state.errors.map(err => <li key={err}>{err}</li>)}
            </ul>
          )
        }
        {
          this.state.folderSize > 0 && (
            <div className="mt-5">
              <h5>Completed!</h5>
              <h5>{this.state.matchingRows}/{this.state.rowCount} rows in {this.state.folderSize} files passed the filter.</h5>
            </div>
          )
        }

      </Container>
    );
  }
}
