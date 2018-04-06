local Probability = bt.Class("Probability",bt.ConditionTask)
bt.Probability = Probability

function Probability:ctor()
    bt.ConditionTask.ctor(self)
    self.name = "Probability"
    self.probability = 0.5
    self.maxValue = 1.0
end

function Probability:init(jsonData)
    bt.ConditionTask:init(jsonData)
    self.probability = jsonData.probability._value
    self.maxValue = jsonData.maxValue._value
end

function Probability:onCheck()
    local value = math.random(0,self.maxValue * 100) / 100
    self:debug(string.format("%.2f/%.2f",value,self.probability))
    return value <= self.probability
end

function Probability:info()
    return string.format("If Probability %d%s",(self.probability/self.maxValue) * 100,"%")
end